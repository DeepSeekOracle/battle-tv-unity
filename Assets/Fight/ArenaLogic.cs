using System;
using System.Collections.Generic;
using BattleTv.Menu;

namespace BattleTv.Fight
{
    /// <summary>
    /// The sand runs the live arena's hands. Book stats, card mods, move shapes,
    /// armor, ward share, and the kit-order cast are the same laws as the main
    /// ring. The pit has no interior stone, so the squad's formation layer is
    /// not copied. The side lane stays in SideDuel.
    /// </summary>
    public static class ArenaLogic
    {
        public const double WardShare = 0.68;
        const double GunPx = 110;

        public static int Casts;
        static Random _rng = new Random(1);
        static readonly List<Pending> _pending = new List<Pending>(8);
        static readonly Dictionary<string, MoveDef> _book = BuildBook();

        sealed class Pending
        {
            public double T;
            public double X;
            public double Y;
            public double R;
            public double Dmg;
            public ArenaBody Src;
            public Status St;
            public bool Strips;
            public string Name;
        }

        public sealed class MoveDef
        {
            public string Id = "";
            public string Name = "";
            public string Shape = "";
            public string Aim = "";
            public double Cd = 8;
            public double Range;
            public double Dmg;
            public double Radius = 3;
            public double Cost = -1;
            public double Drain;
            public int N = 1;
            public bool Lob;
            public bool Strips;
            public bool IgnoreArmor;
            public bool Execute;
            public bool AtRange;
            public Status St;
            public Status Self;
        }

        public sealed class Status
        {
            public double Stun, Root, Silence, Knock, Pull, Aegis, Taunt, Heal, CdReset;
            public double SlowV, SlowD, SwiftV, SwiftD, HasteV, HasteD, RageV, RageD, WeakV, WeakD;
            public double BurnD, BurnT, BleedD, BleedT, RegenD, RegenT, ThornV, ThornT;
            public double WoundV, WoundD, Armor, ArmorT, VeilV, VeilD, LifeV, LifeD;
        }

        public static void Reset(int seed)
        {
            Casts = 0;
            _pending.Clear();
            _rng = new Random(seed <= 0 ? 1 : seed);
        }

        public static void Dress(ArenaBody body, Kit kit)
        {
            double hpMul = 0, dmgMul = 0, rateMul = 0, spdMul = 0, armorAdd = 0, cdAdd = 0;
            double manaMul = 0, manaMaxMul = 0, critAdd = 0, critMulAdd = 0;
            if (kit.Cards != null)
            {
                for (var i = 0; i < kit.Cards.Length; i++)
                    ApplyCard(body, kit.Cards[i], ref hpMul, ref dmgMul, ref rateMul, ref spdMul, ref armorAdd, ref cdAdd, ref manaMul, ref manaMaxMul, ref critAdd, ref critMulAdd);
            }
            var melee = kit.Range <= 1.95;
            var hp = kit.Hp * (1 + hpMul);
            if (melee)
                hp *= 1.18;
            if (!melee && kit.Range > 1.95 && kit.Hp < 560)
                hp += 80;
            body.Max = Math.Max(120, Math.Round(hp));
            body.Hp = body.Max;
            body.Dmg = kit.Dmg * 1.15 * (1 + dmgMul);
            body.Rate = kit.Rate * (1 + rateMul);
            body.Speed = Math.Max(0.35, kit.Speed * (1 + spdMul));
            body.Armor = kit.Armor + armorAdd + (melee ? 1 : 0);
            body.Range = Math.Max(0.8, kit.Range);
            body.ManaMax = Math.Round(kit.Mana * (1 + manaMaxMul));
            body.Mana = body.ManaMax;
            body.ManaRegen = LaneRoster.ManaRegen(kit.Id) * (1 + manaMul);
            body.Crit = Math.Min(0.75, Tune(Crits, kit.Id, 0.12) + critAdd);
            body.CritMul = 1.5 + critMulAdd;
            body.CdMul = Math.Max(0.4, Tune(Cds, kit.Id, 0.9) * (1 + cdAdd));
            body.Gun = body.Range * ArenaMatch.Tile > GunPx;
            body.Splash = kit.Id == "vale" ? 1.4 : 0;
            if (body.StartShield > 0)
                body.Shield = Math.Round(body.Max * body.StartShield);
            var list = new List<MoveDef>(8);
            if (kit.Moves != null)
            {
                for (var i = 0; i < kit.Moves.Length; i++)
                {
                    if (_book.TryGetValue(kit.Moves[i], out var move))
                        list.Add(move);
                }
            }
            if (_book.TryGetValue("stride", out var stride))
            {
                var step = Copy(stride);
                step.Name = body.Gun ? "Slip" : "Close";
                list.Add(step);
            }
            body.Moves = list;
            body.Cds = new double[list.Count];
            body.Cards = Tags(kit);
        }

        public static void Advance(double dt)
        {
            Fly(dt);
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                p.T -= dt;
                if (p.T > 0)
                    continue;
                Blast(p.X, p.Y, p.R, p.Dmg, p.Src, p.St, p.Strips, p.Name);
                _pending.RemoveAt(i);
            }
            var n = ArenaMatch.Bodies.Count;
            for (var i = 0; i < n; i++)
            {
                var body = ArenaMatch.Bodies[i];
                if (!body.Alive)
                {
                    if (body.Token)
                        body.Ttl -= dt;
                    continue;
                }
                if (body.Token)
                {
                    body.Ttl -= dt;
                    if (body.Ttl <= 0)
                    {
                        body.Hp = 0;
                        body.Alive = false;
                        continue;
                    }
                }
                Act(body, dt);
            }
        }

        static void Act(ArenaBody e, double dt)
        {
            e.Swing = Math.Max(0, e.Swing - dt);
            e.AtkCd = Math.Max(0, e.AtkCd - dt);
            Tick(e, dt);
            e.Mana = Math.Min(e.ManaMax, e.Mana + e.ManaRegen * dt);
            e.StrideCd = StrideLeft(e);
            if (e.Stun > 0)
                return;
            var foe = Target(e);
            if (foe == null)
                return;
            // Same rule as the side lane: pinned, with someone in their face,
            // the stride goes through them. It does not wait on the gun step.
            if (TryCornerLeap(e))
                return;
            if (e.Hp / e.Max < 0.5 && TrySelfHeal(e))
                return;
            if (TryStopRush(e))
                return;
            if (TryStride(e, foe))
                return;
            if (TryKit(e, foe))
                return;
            var dx = foe.X - e.X;
            var dy = foe.Y - e.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001)
                dist = 0.001;
            double dmg, speed, range;
            Buff(e, out dmg, out speed, out range);
            var band = e.Gun ? Math.Max(range * 0.9, 58) : range;
            var standing = e.Gun ? dist <= band + 18 : dist <= range + 10;
            var walked = 0.0;
            if (!standing && e.Root <= 0 && speed > 0)
            {
                var step = speed * dt;
                if (step > dist - (e.Gun ? band : range))
                    step = Math.Max(0, dist - (e.Gun ? band : range));
                walked = step;
                e.X += dx / dist * step;
                e.Y += dy / dist * step;
                ArenaMatch.Hold(e);
                dx = foe.X - e.X;
                dy = foe.Y - e.Y;
                dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 0.001)
                    dist = 0.001;
            }
            if (e.AtkCd <= 0 && e.Silence <= 0 && dist <= range + 16)
                Attack(e, foe, dmg, dx, dy, walked > 10);
        }

        static void Attack(ArenaBody e, ArenaBody foe, double dmg, double dx, double dy, bool bash)
        {
            e.AtkCd = 1.0 / Math.Max(0.15, BuffRate(e));
            e.Swing = 0.4;
            e.Callout = e.Id == "vale" && bash ? "Shield Bash" : e.Atk;
            Casts++;
            if (e.Gun)
            {
                var ang = Math.Atan2(dy, dx);
                ArenaMatch.Bolts.Add(new ArenaBolt
                {
                    X = e.X,
                    Y = e.Y,
                    Angle = ang,
                    Speed = 420,
                    Life = 2,
                    Team = e.Team,
                    Dmg = dmg,
                    Src = e,
                    Name = e.Atk
                });
                return;
            }
            Hit(e, foe, dmg, true, false, false, e.Atk);
            if (e.Splash > 0)
            {
                var r = e.Splash * ArenaMatch.Tile;
                for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                {
                    var other = ArenaMatch.Bodies[i];
                    if (!other.Alive || other.Team == e.Team || other == foe)
                        continue;
                    if (Dist(foe, other) < r)
                        Hit(e, other, dmg * 0.45, true, false, true, e.Atk);
                }
            }
        }

        static bool TryKit(ArenaBody e, ArenaBody foe)
        {
            if (e.Moves == null)
                return false;
            var dist = Dist(e, foe);
            for (var i = 0; i < e.Moves.Count; i++)
            {
                var mv = e.Moves[i];
                if (mv.Shape == "stride" || e.Cds[i] > 0 || e.Mana < Cost(mv))
                    continue;
                var reach = mv.Range * ArenaMatch.Tile;
                var ring = mv.Radius * ArenaMatch.Tile;
                var ready = true;
                if (mv.Aim == "allylow")
                {
                    var low = Lowest(e);
                    ready = low != null && low != e && low.Hp / low.Max < 0.8;
                }
                else if (mv.Aim == "allies" || mv.Shape == "allies")
                    ready = e.Hp / e.Max < 0.92 || dist < Math.Max(reach, 5 * ArenaMatch.Tile);
                else if (mv.Aim == "deadally")
                    ready = AnyDead(e.Team);
                else if (mv.Aim == "self" || mv.Shape == "aura" || mv.Shape == "self")
                {
                    var span = Math.Max(reach, ring);
                    // A self plate with no radius was waiting for an 8px overlap.
                    // The lane casts those when the other fighter is within 3 tiles.
                    ready = span < 8 ? dist < 3 * ArenaMatch.Tile : dist < span + 8;
                    if (ready && mv.St != null && mv.St.Aegis > 0 && e.Shield > 0 && mv.Shape == "self")
                        ready = false;
                }
                else
                    ready = dist <= reach + 30 || mv.Range <= 0 && dist < ring + 8;
                if (ready && Cast(e, mv, i, foe))
                    return true;
            }
            return false;
        }

        const double PinBand = 22;

        static bool TryCornerLeap(ArenaBody e)
        {
            if (e.Root > 0 || e.Silence > 0 || !AgainstStone(e))
                return false;
            var si = IndexOf(e, "stride");
            if (si < 0 || e.Cds[si] > 0 || e.Mana < Cost(e.Moves[si]))
                return false;
            var face = (Math.Min(e.Range, 2.2) + 1.5) * ArenaMatch.Tile;
            ArenaBody foe = null;
            var best = face;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team)
                    continue;
                var d = Dist(e, o);
                if (d > best || !Inward(e, o))
                    continue;
                best = d;
                foe = o;
            }
            if (foe == null)
                return false;
            var dx = foe.X - e.X;
            var dy = foe.Y - e.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1)
                dist = 1;
            var gap = (Math.Min(Math.Max(e.Range, foe.Range), 2.4) + 0.85) * ArenaMatch.Tile;
            var landX = foe.X + dx / dist * gap;
            var landY = foe.Y + dy / dist * gap;
            var held = new ArenaBody { X = landX, Y = landY };
            ArenaMatch.Hold(held);
            if ((held.X - foe.X) * dx + (held.Y - foe.Y) * dy <= 0)
                return false;
            var mv = e.Moves[si];
            e.X = held.X;
            e.Y = held.Y;
            e.Mana -= Cost(mv);
            e.Cds[si] = mv.Cd;
            e.StrideCd = e.Cds[si];
            e.Callout = mv.Name;
            e.Swing = 0.4;
            Casts++;
            return true;
        }

        static bool AgainstStone(ArenaBody e)
        {
            return e.X <= ArenaMatch.Inset + PinBand
                || e.Y <= ArenaMatch.Inset + PinBand
                || e.X >= ArenaMatch.WorldW - ArenaMatch.Inset - PinBand
                || e.Y >= ArenaMatch.WorldH - ArenaMatch.Inset - PinBand;
        }

        static bool Inward(ArenaBody e, ArenaBody o)
        {
            if (e.X <= ArenaMatch.Inset + PinBand && o.X <= e.X + 6)
                return false;
            if (e.X >= ArenaMatch.WorldW - ArenaMatch.Inset - PinBand && o.X >= e.X - 6)
                return false;
            if (e.Y <= ArenaMatch.Inset + PinBand && o.Y <= e.Y + 6)
                return false;
            if (e.Y >= ArenaMatch.WorldH - ArenaMatch.Inset - PinBand && o.Y >= e.Y - 6)
                return false;
            return true;
        }

        static bool TryStride(ArenaBody e, ArenaBody foe)
        {
            var si = IndexOf(e, "stride");
            if (si < 0 || e.Cds[si] > 0 || e.Mana < Cost(e.Moves[si]))
                return false;
            if (HasReadyBlink(e, si))
                return false;
            if (!e.Gun)
            {
                if (foe.Gun)
                {
                    var d = Dist(e, foe);
                    var reach = (e.Moves[si].Range) * ArenaMatch.Tile + e.Range * ArenaMatch.Tile;
                    if (d > e.Range * ArenaMatch.Tile + 48 && d <= reach)
                        return Cast(e, e.Moves[si], si, foe);
                }
                return false;
            }
            ArenaBody fist = null;
            var best = 150.0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team || o.Gun || o.Root > 0 || o.Stun > 0)
                    continue;
                var d = Dist(e, o);
                if (d < best)
                {
                    best = d;
                    fist = o;
                }
            }
            if (fist != null)
                return Cast(e, e.Moves[si], si, fist);
            return false;
        }

        static bool TryStopRush(ArenaBody e)
        {
            ArenaBody near = null;
            var best = 132.0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team || o.Gun)
                    continue;
                var d = Dist(e, o);
                if (d < best)
                {
                    best = d;
                    near = o;
                    if (o.Root > 0 || o.Stun > 0 || o.SlowT > 0)
                        return false;
                }
            }
            if (near == null || e.Moves == null)
                return false;
            var pick = -1;
            var score = 0.0;
            for (var i = 0; i < e.Moves.Count; i++)
            {
                var mv = e.Moves[i];
                if (e.Cds[i] > 0 || e.Mana < Cost(mv) || mv.St == null)
                    continue;
                var cc = (mv.St.Root > 0 ? 5 : 0) + (mv.St.Stun > 0 ? 4.4 : 0) + (mv.St.Knock > 0 ? 3.4 : 0) + (mv.St.SlowD > 0 ? 3 : 0) + (mv.St.Silence > 0 ? 2.2 : 0);
                if (cc <= score)
                    continue;
                var wide = mv.Shape == "self" || mv.Shape == "aura" || mv.Shape == "allies";
                var reach = Math.Max(mv.Range * ArenaMatch.Tile, wide ? mv.Radius * ArenaMatch.Tile : 0);
                if (Dist(e, near) > reach + 34)
                    continue;
                score = cc;
                pick = i;
            }
            return pick >= 0 && Cast(e, e.Moves[pick], pick, near);
        }

        static bool TrySelfHeal(ArenaBody e)
        {
            if (e.Moves == null)
                return false;
            for (var i = 0; i < e.Moves.Count; i++)
            {
                var mv = e.Moves[i];
                if (mv.St == null || mv.St.Heal <= 0)
                    continue;
                if (mv.Aim != "self" && mv.Shape != "self")
                    continue;
                if (e.Cds[i] > 0 || e.Mana < Cost(mv))
                    continue;
                return Cast(e, mv, i, e);
            }
            return false;
        }

        static bool Cast(ArenaBody e, MoveDef mv, int idx, ArenaBody hint)
        {
            if (e.Silence > 0 || e.Stun > 0 || e.Mana < Cost(mv))
                return false;
            ArenaBody land = null;
            if (mv.Shape == "stride")
            {
                land = StrideLand(e, hint);
                if (land == null)
                    return false;
            }
            if (mv.Shape == "revive" && !AnyDead(e.Team))
                return false;
            e.Mana -= Cost(mv);
            e.Cds[idx] = mv.Shape == "stride" ? mv.Cd : mv.Cd * e.CdMul;
            e.StrideCd = StrideLeft(e);
            e.Callout = mv.Name;
            e.Swing = 0.42;
            Casts++;
            var opening = mv.Dmg > 0 && e.FirstDouble && !e.FirstUsed;
            if (opening)
                e.FirstUsed = true;
            var tgt = hint;
            if (mv.Aim == "weakest" || mv.Aim == "enemy" || mv.Aim == "clump")
                tgt = Pick(e, mv.Aim) ?? hint;
            switch (mv.Shape)
            {
                case "melee":
                case "arc":
                    Strike(e, mv, opening);
                    break;
                case "aura":
                    Aura(e, mv, opening);
                    break;
                case "blast":
                    var p = tgt ?? e;
                    _pending.Add(new Pending
                    {
                        T = 0.32,
                        X = p.X,
                        Y = p.Y,
                        R = (mv.Radius > 0 ? mv.Radius : 2) * ArenaMatch.Tile,
                        Dmg = e.Dmg * mv.Dmg,
                        Src = e,
                        St = mv.St,
                        Strips = mv.Strips,
                        Name = mv.Name
                    });
                    if (mv.Self != null)
                        Apply(e, mv.Self, e, false);
                    break;
                case "beam":
                    Beam(e, tgt ?? e, mv, opening);
                    break;
                case "bolt":
                case "multi":
                case "lob":
                    Shoot(e, tgt ?? e, mv, opening);
                    break;
                case "self":
                    Apply(e, mv.St, e, false);
                    break;
                case "ally":
                    var low = Lowest(e);
                    if (low != null)
                        Apply(low, mv.St, e, false);
                    break;
                case "allies":
                    var radius = (mv.Range > 0 ? mv.Range : 6) * ArenaMatch.Tile;
                    for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                    {
                        var o = ArenaMatch.Bodies[i];
                        if (o.Alive && o.Team == e.Team && Dist(e, o) <= radius)
                            Apply(o, mv.St, e, false);
                    }
                    break;
                case "blink":
                    Blink(e, tgt, mv, opening);
                    break;
                case "stride":
                    e.X = land.X;
                    e.Y = land.Y;
                    ArenaMatch.Hold(e);
                    break;
                case "revive":
                    Revive(e, mv);
                    break;
                case "summon":
                    Summon(e, mv);
                    break;
            }
            return true;
        }

        static void Strike(ArenaBody e, MoveDef mv, bool opening)
        {
            var radius = Math.Max(8, mv.Range * ArenaMatch.Tile);
            var any = false;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team || Dist(e, o) > radius)
                    continue;
                any = true;
                var dealt = Hit(e, o, e.Dmg * mv.Dmg, true, mv.IgnoreArmor, false, mv.Name, opening, mv.Execute);
                if (mv.Drain > 0 && dealt > 0)
                    Heal(e, dealt * mv.Drain);
                Apply(o, mv.St, e, true);
            }
            if (!any && mv.Self != null)
                Apply(e, mv.Self, e, false);
        }

        static void Aura(ArenaBody e, MoveDef mv, bool opening)
        {
            var radius = mv.Radius * ArenaMatch.Tile;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team || Dist(e, o) > radius)
                    continue;
                if (mv.Dmg > 0)
                    Hit(e, o, e.Dmg * mv.Dmg, true, mv.IgnoreArmor, false, mv.Name, opening, false);
                Apply(o, mv.St, e, true);
            }
            if (mv.Self != null)
                Apply(e, mv.Self, e, false);
            if (mv.St != null && mv.St.Aegis > 0)
                Apply(e, mv.St, e, false);
        }

        static void Beam(ArenaBody e, ArenaBody tgt, MoveDef mv, bool opening)
        {
            var ang = Math.Atan2(tgt.Y - e.Y, tgt.X - e.X);
            var len = mv.Range * ArenaMatch.Tile;
            var cos = Math.Cos(ang);
            var sin = Math.Sin(ang);
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team)
                    continue;
                var dx = o.X - e.X;
                var dy = o.Y - e.Y;
                var along = dx * cos + dy * sin;
                if (along < 0 || along > len)
                    continue;
                if (Math.Abs(-dx * sin + dy * cos) > 14)
                    continue;
                Hit(e, o, e.Dmg * mv.Dmg, false, mv.IgnoreArmor, false, mv.Name, opening, false);
                Apply(o, mv.St, e, true);
            }
        }

        static void Shoot(ArenaBody e, ArenaBody tgt, MoveDef mv, bool opening)
        {
            var n = Math.Max(1, mv.N);
            var ang0 = Math.Atan2(tgt.Y - e.Y, tgt.X - e.X);
            var depth = Dist(e, tgt);
            if (depth < 1)
                depth = ArenaMatch.Tile;
            for (var i = 0; i < n; i++)
            {
                var spread = n > 1 ? (i - (n - 1) / 2.0) * 0.24 : 0;
                var ang = ang0 + spread;
                var lob = mv.Lob || mv.Shape == "lob";
                ArenaMatch.Bolts.Add(new ArenaBolt
                {
                    X = e.X,
                    Y = e.Y,
                    Angle = ang,
                    Speed = lob ? 200 : 430,
                    Life = lob ? 3 : 2.6,
                    Team = e.Team,
                    Dmg = e.Dmg * mv.Dmg,
                    Src = e,
                    Name = mv.Name,
                    Lob = lob,
                    Tx = lob ? e.X + Math.Cos(ang) * depth : tgt.X,
                    Ty = lob ? e.Y + Math.Sin(ang) * depth : tgt.Y,
                    Radius = (mv.Radius > 0 ? mv.Radius : 1.4) * ArenaMatch.Tile,
                    St = mv.St,
                    Opening = opening,
                    IgnoreArmor = mv.IgnoreArmor
                });
            }
        }

        static void Blink(ArenaBody e, ArenaBody tgt, MoveDef mv, bool opening)
        {
            if (tgt == null || tgt == e)
                tgt = Pick(e, "weakest");
            if (tgt == null)
                return;
            var ang = Math.Atan2(e.Y - tgt.Y, e.X - tgt.X);
            e.X = tgt.X + Math.Cos(ang) * 26;
            e.Y = tgt.Y + Math.Sin(ang) * 26;
            ArenaMatch.Hold(e);
            if (mv.Dmg > 0)
            {
                var dealt = Hit(e, tgt, e.Dmg * mv.Dmg, true, false, false, mv.Name, opening, mv.Execute);
                if (mv.Drain > 0 && dealt > 0)
                    Heal(e, dealt * mv.Drain);
            }
            Apply(tgt, mv.St, e, true);
            if (mv.Self != null)
                Apply(e, mv.Self, e, false);
        }

        static ArenaBody StrideLand(ArenaBody e, ArenaBody prefer)
        {
            if (!e.Gun)
            {
                var window = 7 * ArenaMatch.Tile + 16;
                var tgt = prefer != null && prefer.Alive && prefer.Team != e.Team && Dist(e, prefer) <= window ? prefer : null;
                if (tgt == null)
                {
                    var n = Target(e);
                    if (n != null && Dist(e, n) <= window)
                        tgt = n;
                }
                if (tgt == null)
                    return null;
                var ang = Math.Atan2(e.Y - tgt.Y, e.X - tgt.X);
                var spot = new ArenaBody { X = tgt.X + Math.Cos(ang) * 42, Y = tgt.Y + Math.Sin(ang) * 42 };
                if (Dist(e, tgt) - 42 < 48)
                    return null;
                return spot;
            }
            ArenaBody fist = null;
            var best = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team)
                    continue;
                var d = Dist(e, o);
                if (o.Gun)
                    d += 400;
                if (d < best)
                {
                    best = d;
                    fist = o;
                }
            }
            if (fist == null)
                return null;
            var away = Dist(e, fist);
            if (away < 1)
                away = 1;
            var back = 5.2 * ArenaMatch.Tile;
            return new ArenaBody
            {
                X = fist.X + (e.X - fist.X) / away * (away + back),
                Y = fist.Y + (e.Y - fist.Y) / away * (away + back)
            };
        }

        static void Revive(ArenaBody e, MoveDef mv)
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (o.Alive || o.Team != e.Team || o.Token)
                    continue;
                o.Alive = true;
                o.Hp = Math.Round(o.Max * (mv.Dmg > 0 ? mv.Dmg : 0.3));
                o.Shield += Math.Round(o.Max * 0.1);
                o.Stun = 0;
                o.Root = 0;
                return;
            }
        }

        static int _tokenSerial;

        static void Summon(ArenaBody e, MoveDef mv)
        {
            var n = Math.Max(1, mv.N);
            for (var i = 0; i < n && ArenaMatch.Bodies.Count < 16; i++)
            {
                var ward = new ArenaBody
                {
                    Id = "ward_" + (++_tokenSerial),
                    Name = "Warden",
                    Atk = "Warden Bolt",
                    Team = e.Team,
                    X = e.X + (i == 0 ? -28 : 28),
                    Y = e.Y + 12,
                    Hp = 170,
                    Max = 170,
                    Dmg = 16 * 1.15,
                    Rate = 0.9,
                    Speed = 2.4,
                    Armor = 1,
                    Range = 7,
                    Mana = 30,
                    ManaMax = 30,
                    ManaRegen = 4,
                    Gun = true,
                    Token = true,
                    Ttl = 22,
                    CdMul = 1,
                    Crit = 0.1,
                    CritMul = 1.5,
                    Alive = true
                };
                ArenaMatch.Hold(ward);
                ArenaMatch.Bodies.Add(ward);
            }
        }

        public static double Hit(ArenaBody src, ArenaBody tgt, double amount, bool melee, bool ignoreArmor, bool dot, string name, bool opening = false, bool execute = false)
        {
            if (tgt == null || !tgt.Alive)
                return 0;
            var dmg = amount;
            var veil = tgt.Veil + (tgt.VeilT > 0 ? tgt.VeilV : 0);
            if (!dot && veil > 0 && _rng.NextDouble() < veil)
                return 0;
            if (src != null && !dot && _rng.NextDouble() < src.Crit)
                dmg *= src.CritMul;
            if (opening)
                dmg *= 2;
            var exec = src != null ? src.Execute : 0;
            if (execute)
                exec = Math.Max(exec, 0.7);
            if (exec > 0 && tgt.Max > 0 && tgt.Hp / tgt.Max < 0.32)
                dmg *= 1 + exec;
            if (tgt.Bulwark > 0 && tgt.Max > 0 && tgt.Hp / tgt.Max > 0.6)
                dmg *= 1 - tgt.Bulwark;
            if (!dot)
                dmg *= SideDuel.SuddenScale(ArenaMatch.Time);
            if (!ignoreArmor)
            {
                double aura = 0;
                for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                {
                    var a = ArenaMatch.Bodies[i];
                    if (a.Alive && a.Team == tgt.Team && a != tgt)
                        aura += a.GivesArmor;
                }
                dmg -= (tgt.Armor + tgt.ArmorBuff + aura) * 1.1;
                if (dmg < 0.5)
                    return 0;
            }
            if (tgt.Shield > 0)
            {
                var incoming = dmg;
                var absorbed = Math.Min(tgt.Shield, incoming * WardShare);
                tgt.Shield -= absorbed;
                dmg -= absorbed;
                if (dmg < 0.5 && !dot)
                    return 0;
            }
            dmg = Math.Round(dmg);
            if (dmg <= 0)
                return 0;
            if (!tgt.UsedLastStand && tgt.LastStand && tgt.Hp - dmg > 0 && (tgt.Hp - dmg) / tgt.Max < 0.3 && tgt.Hp / tgt.Max >= 0.3)
            {
                tgt.UsedLastStand = true;
                tgt.Hp = Math.Min(tgt.Max, tgt.Hp - dmg + tgt.Max * 0.35);
            }
            else
                tgt.Hp -= dmg;
            if (src != null && src.Leech > 0)
                Heal(src, dmg * src.Leech);
            if (src != null && src.LifestealT > 0)
                Heal(src, dmg * src.LifestealV);
            if (src != null && melee && !dot)
            {
                var back = dmg * tgt.ThornsMod;
                if (tgt.ThornsT > 0)
                    back += dmg * tgt.ThornsV;
                if (back > 1)
                    Hit(tgt, src, back, false, true, true, "thorns");
            }
            if (tgt.Hp <= 0)
            {
                if (!tgt.UsedRevive && tgt.SecondLife)
                {
                    tgt.UsedRevive = true;
                    tgt.Hp = Math.Round(tgt.Max * 0.28);
                    tgt.Shield += Math.Round(tgt.Max * 0.08);
                    tgt.Alive = true;
                }
                else
                {
                    tgt.Hp = 0;
                    tgt.Alive = false;
                    tgt.Callout = "";
                    if (src != null && src.Vamp > 0)
                        Heal(src, src.Max * src.Vamp);
                }
            }
            return dmg;
        }

        static void Blast(double x, double y, double radius, double dmg, ArenaBody src, Status st, bool strips, string name)
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || (src != null && o.Team == src.Team))
                    continue;
                var dx = o.X - x;
                var dy = o.Y - y;
                if (dx * dx + dy * dy > radius * radius)
                    continue;
                if (strips)
                    o.Shield = 0;
                if (dmg > 0)
                    Hit(src, o, strips ? dmg * 2 : dmg, false, false, false, name);
                Apply(o, st, src, true);
            }
        }

        static void Fly(double dt)
        {
            for (var i = ArenaMatch.Bolts.Count - 1; i >= 0; i--)
            {
                var bolt = ArenaMatch.Bolts[i];
                if (bolt.Speed <= 0)
                {
                    var aim = bolt.Target;
                    if (aim != null && aim.Alive)
                    {
                        var dx = aim.X - bolt.X;
                        var dy = aim.Y - bolt.Y;
                        var dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < 18)
                        {
                            Hit(bolt.Src, aim, bolt.Dmg, false, bolt.IgnoreArmor, false, bolt.Name, bolt.Opening, false);
                            ArenaMatch.Bolts.RemoveAt(i);
                            continue;
                        }
                        var step = 14 * ArenaMatch.Tile * dt;
                        bolt.X += dx / dist * step;
                        bolt.Y += dy / dist * step;
                    }
                    else
                        ArenaMatch.Bolts.RemoveAt(i);
                    continue;
                }
                bolt.Life -= dt;
                bolt.X += Math.Cos(bolt.Angle) * bolt.Speed * dt;
                bolt.Y += Math.Sin(bolt.Angle) * bolt.Speed * dt;
                if (bolt.Lob)
                {
                    var dx = bolt.Tx - bolt.X;
                    var dy = bolt.Ty - bolt.Y;
                    if (dx * dx + dy * dy < 18 * 18 || bolt.Life <= 0)
                    {
                        Blast(bolt.X, bolt.Y, bolt.Radius, bolt.Dmg, bolt.Src, bolt.St, false, bolt.Name);
                        ArenaMatch.Bolts.RemoveAt(i);
                    }
                    continue;
                }
                var hit = false;
                for (var b = 0; b < ArenaMatch.Bodies.Count; b++)
                {
                    var o = ArenaMatch.Bodies[b];
                    if (!o.Alive || o.Team == bolt.Team)
                        continue;
                    var dx = o.X - bolt.X;
                    var dy = o.Y - bolt.Y;
                    if (dx * dx + dy * dy < 22 * 22)
                    {
                        Hit(bolt.Src, o, bolt.Dmg, false, bolt.IgnoreArmor, false, bolt.Name, bolt.Opening, false);
                        Apply(o, bolt.St, bolt.Src, true);
                        hit = true;
                        break;
                    }
                }
                if (hit || bolt.Life <= 0 || bolt.X < 0 || bolt.Y < 0 || bolt.X > ArenaMatch.WorldW || bolt.Y > ArenaMatch.WorldH)
                    ArenaMatch.Bolts.RemoveAt(i);
            }
        }

        static void Apply(ArenaBody e, Status st, ArenaBody from, bool hostile)
        {
            if (e == null || st == null)
                return;
            if (hostile && from != null && from != e && !e.UsedCleanse && e.CleanseCard && (st.Stun > 0 || st.Root > 0 || st.Silence > 0))
            {
                e.UsedCleanse = true;
                return;
            }
            var hard = hostile && (st.Stun > 0 || st.Root > 0 || st.Silence > 0 || st.Pull > 0 || st.Knock > 0);
            var soft = hostile && (st.SlowD > 0 || st.WeakD > 0);
            var hs = hard ? Dr(e, true) : 1;
            var ss = soft ? Dr(e, false) : 1;
            if (st.Stun > 0)
                e.Stun = Math.Max(e.Stun, Math.Max(0.25, st.Stun * hs));
            if (st.Root > 0)
                e.Root = Math.Max(e.Root, Math.Max(0.25, st.Root * hs));
            if (st.Silence > 0)
                e.Silence = Math.Max(e.Silence, Math.Max(0.25, st.Silence * hs));
            if (st.SlowD > 0)
            {
                e.SlowT = Math.Max(e.SlowT, st.SlowD);
                e.SlowV = Math.Max(e.SlowV, st.SlowV * ss);
            }
            if (st.SwiftD > 0)
            {
                e.SwiftT = Math.Max(e.SwiftT, st.SwiftD);
                e.SwiftV = Math.Max(e.SwiftV, st.SwiftV);
            }
            if (st.HasteD > 0)
            {
                e.HasteT = Math.Max(e.HasteT, st.HasteD);
                e.HasteV = Math.Max(e.HasteV, st.HasteV);
            }
            if (st.RageD > 0)
            {
                e.RageT = Math.Max(e.RageT, st.RageD);
                e.RageV = Math.Max(e.RageV, st.RageV);
            }
            if (st.WeakD > 0)
            {
                e.WeakT = Math.Max(e.WeakT, st.WeakD);
                e.WeakV = Math.Max(e.WeakV, st.WeakV * ss);
            }
            if (st.BurnT > 0)
            {
                e.BurnT = Math.Max(e.BurnT, st.BurnT);
                e.BurnDps = Math.Max(e.BurnDps, st.BurnD);
            }
            if (st.BleedT > 0)
            {
                e.BleedT = Math.Max(e.BleedT, st.BleedT);
                e.BleedDps = Math.Max(e.BleedDps, st.BleedD);
            }
            if (st.RegenT > 0)
            {
                e.RegenT = Math.Max(e.RegenT, st.RegenT);
                e.RegenHps = Math.Max(e.RegenHps, st.RegenD);
            }
            if (st.ThornT > 0)
            {
                e.ThornsT = Math.Max(e.ThornsT, st.ThornT);
                e.ThornsV = Math.Max(e.ThornsV, st.ThornV);
            }
            if (st.WoundD > 0)
            {
                e.WoundT = Math.Max(e.WoundT, st.WoundD);
                e.WoundV = Math.Max(e.WoundV, st.WoundV);
            }
            if (st.VeilD > 0)
            {
                e.VeilT = Math.Max(e.VeilT, st.VeilD);
                e.VeilV = Math.Max(e.VeilV, st.VeilV);
            }
            if (st.LifeD > 0)
            {
                e.LifestealT = Math.Max(e.LifestealT, st.LifeD);
                e.LifestealV = Math.Max(e.LifestealV, st.LifeV);
            }
            if (st.Aegis > 0 && e.Shield <= 0)
                e.Shield += Math.Round(e.Max * st.Aegis);
            if (st.Taunt > 0)
                e.TauntT = Math.Max(e.TauntT, st.Taunt);
            if (st.ArmorT > 0)
            {
                e.ArmorBuff = Math.Max(e.ArmorBuff, st.Armor);
                e.ArmorBuffT = Math.Max(e.ArmorBuffT, st.ArmorT);
            }
            if (st.Heal > 0)
                Heal(e, e.Max * st.Heal);
            if (st.CdReset > 0 && e.Cds != null)
            {
                for (var i = 0; i < e.Cds.Length; i++)
                    e.Cds[i] = 0.15;
            }
            if (st.Knock > 0 && from != null)
                Shove(e, from, Math.Max(0.5, st.Knock * hs) * ArenaMatch.Tile, false);
            if (st.Pull > 0 && from != null)
                Shove(e, from, Math.Max(0.5, st.Pull * hs) * ArenaMatch.Tile, true);
        }

        static void Shove(ArenaBody e, ArenaBody from, double px, bool pull)
        {
            var ang = Math.Atan2(pull ? from.Y - e.Y : e.Y - from.Y, pull ? from.X - e.X : e.X - from.X);
            e.X += Math.Cos(ang) * px;
            e.Y += Math.Sin(ang) * px;
            ArenaMatch.Hold(e);
        }

        static double Dr(ArenaBody e, bool hard)
        {
            var now = ArenaMatch.Time;
            if (hard)
            {
                if (now - e.CcHardAt > 5)
                    e.CcHardN = 0;
                e.CcHardN++;
                e.CcHardAt = now;
                if (e.CcHardN <= 1)
                    return 1;
                if (e.CcHardN == 2)
                    return 0.6;
                return 0.35;
            }
            if (now - e.CcSoftAt > 5)
                e.CcSoftN = 0;
            e.CcSoftN++;
            e.CcSoftAt = now;
            if (e.CcSoftN <= 1)
                return 1;
            if (e.CcSoftN == 2)
                return 0.8;
            return 0.6;
        }

        static void Heal(ArenaBody e, double amt)
        {
            if (e == null || !e.Alive || amt <= 0)
                return;
            if (e.WoundT > 0)
                amt -= amt * e.WoundV;
            if (amt > 0)
                e.Hp = Math.Min(e.Max, e.Hp + amt);
        }

        static void Tick(ArenaBody e, double dt)
        {
            e.Stun = Math.Max(0, e.Stun - dt);
            e.Root = Math.Max(0, e.Root - dt);
            e.Silence = Math.Max(0, e.Silence - dt);
            e.SlowT = Math.Max(0, e.SlowT - dt);
            e.SwiftT = Math.Max(0, e.SwiftT - dt);
            e.HasteT = Math.Max(0, e.HasteT - dt);
            e.RageT = Math.Max(0, e.RageT - dt);
            e.WeakT = Math.Max(0, e.WeakT - dt);
            e.TauntT = Math.Max(0, e.TauntT - dt);
            e.ThornsT = Math.Max(0, e.ThornsT - dt);
            e.WoundT = Math.Max(0, e.WoundT - dt);
            e.VeilT = Math.Max(0, e.VeilT - dt);
            e.LifestealT = Math.Max(0, e.LifestealT - dt);
            e.ArmorBuffT = Math.Max(0, e.ArmorBuffT - dt);
            if (e.ArmorBuffT <= 0)
                e.ArmorBuff = 0;
            if (e.SlowT <= 0)
                e.SlowV = 0;
            if (e.Cds != null)
            {
                for (var i = 0; i < e.Cds.Length; i++)
                    e.Cds[i] = Math.Max(0, e.Cds[i] - dt);
            }
            Soak(e, ref e.BurnT, ref e.BurnDps, dt);
            Soak(e, ref e.BleedT, ref e.BleedDps, dt);
            if (e.RegenT > 0)
            {
                e.RegenT -= dt;
                Heal(e, e.RegenHps * dt);
                if (e.RegenT <= 0)
                    e.RegenHps = 0;
            }
            if (e.PassiveRegen > 0)
                Heal(e, e.PassiveRegen * dt);
        }

        static void Soak(ArenaBody e, ref double t, ref double dps, double dt)
        {
            if (t <= 0)
                return;
            t -= dt;
            var bite = dps * dt;
            if (bite > 0)
                Hit(null, e, bite, false, true, true, "");
            if (t <= 0)
                dps = 0;
        }

        static void Buff(ArenaBody e, out double dmg, out double speed, out double range)
        {
            dmg = e.Dmg;
            if (e.RageT > 0)
                dmg *= 1 + e.RageV;
            if (e.WeakT > 0)
                dmg *= 1 - e.WeakV;
            if (e.Fury > 0 && e.Max > 0 && e.Hp / e.Max < 0.4)
                dmg *= 1 + e.Fury;
            double aura = 0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var a = ArenaMatch.Bodies[i];
                if (a.Alive && a.Team == e.Team && a != e)
                    aura += a.GivesDmg;
            }
            dmg *= 1 + aura;
            var slow = e.SlowT > 0 ? e.SlowV : 0;
            var swift = e.SwiftT > 0 ? e.SwiftV : 0;
            speed = e.Speed * ArenaMatch.Tile * (1 + swift) * (1 - slow);
            if (e.Stun > 0 || e.Root > 0)
                speed = 0;
            range = e.Range * ArenaMatch.Tile + (e.TauntT > 0 ? 12 : 0);
        }

        static double BuffRate(ArenaBody e)
        {
            return e.Rate * (1 + (e.HasteT > 0 ? e.HasteV : 0));
        }

        static ArenaBody Target(ArenaBody e)
        {
            ArenaBody taunt = null;
            var bestT = double.MaxValue;
            ArenaBody near = null;
            var best = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team)
                    continue;
                var d = Dist(e, o);
                if (o.TauntT > 0 && d < bestT)
                {
                    bestT = d;
                    taunt = o;
                }
                if (d < best)
                {
                    best = d;
                    near = o;
                }
            }
            return taunt ?? near;
        }

        static ArenaBody Pick(ArenaBody e, string aim)
        {
            ArenaBody best = null;
            var score = aim == "weakest" ? double.MaxValue : double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team == e.Team)
                    continue;
                var s = aim == "weakest" ? o.Hp / Math.Max(1, o.Max) : Dist(e, o);
                if (s < score)
                {
                    score = s;
                    best = o;
                }
            }
            return best;
        }

        static ArenaBody Lowest(ArenaBody e)
        {
            ArenaBody best = null;
            var score = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive || o.Team != e.Team)
                    continue;
                var s = o.Hp / Math.Max(1, o.Max);
                if (s < score)
                {
                    score = s;
                    best = o;
                }
            }
            return best;
        }

        static bool AnyDead(int team)
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var o = ArenaMatch.Bodies[i];
                if (!o.Alive && o.Team == team && !o.Token)
                    return true;
            }
            return false;
        }

        static int IndexOf(ArenaBody e, string shape)
        {
            if (e.Moves == null)
                return -1;
            for (var i = 0; i < e.Moves.Count; i++)
            {
                if (e.Moves[i].Shape == shape)
                    return i;
            }
            return -1;
        }

        static bool HasReadyBlink(ArenaBody e, int skip)
        {
            for (var i = 0; i < e.Moves.Count; i++)
            {
                if (i == skip || e.Moves[i].Shape != "blink")
                    continue;
                if (e.Cds[i] <= 0 && e.Mana >= Cost(e.Moves[i]))
                    return true;
            }
            return false;
        }

        static double StrideLeft(ArenaBody e)
        {
            var i = IndexOf(e, "stride");
            if (i < 0 || e.Cds == null)
                return 0;
            return e.Cds[i];
        }

        static double Cost(MoveDef mv)
        {
            if (mv.Cost >= 0)
                return mv.Cost;
            return Math.Min(mv.Cd * 1.1, 40);
        }

        static double Dist(ArenaBody a, ArenaBody b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        static void ApplyCard(ArenaBody body, string id, ref double hpMul, ref double dmgMul, ref double rateMul, ref double spdMul, ref double armorAdd, ref double cdAdd, ref double manaMul, ref double manaMaxMul, ref double critAdd, ref double critMulAdd)
        {
            switch (id)
            {
                case "might": dmgMul += 0.25; break;
                case "iron": armorAdd += 3; break;
                case "heart": hpMul += 0.25; break;
                case "haste": rateMul += 0.22; cdAdd += -0.15; break;
                case "swift": spdMul += 0.18; break;
                case "leech": body.Leech += 0.15; break;
                case "thorns": body.ThornsMod += 0.30; break;
                case "regen": body.PassiveRegen += 2.2; break;
                case "veil": body.Veil += 0.18; break;
                case "fury": body.Fury += 0.45; break;
                case "laststand": body.LastStand = true; break;
                case "warded": body.StartShield += 0.30; break;
                case "secondlife": body.SecondLife = true; break;
                case "pierce": body.Pierce += 1; break;
                case "volley": body.Volley += 1; break;
                case "echo": body.Echo += 0.40; break;
                case "bulwark": body.Bulwark += 0.22; break;
                case "executioner": body.Execute += 0.70; break;
                case "giant": hpMul += 0.40; spdMul += -0.12; break;
                case "nimble": hpMul += -0.15; spdMul += 0.25; break;
                case "vampiric": body.Vamp += 0.22; break;
                case "manaflow": manaMul += 0.60; manaMaxMul += 0.20; cdAdd += -0.08; break;
                case "overclock": cdAdd += -0.32; hpMul += -0.10; break;
                case "aegisgift": body.GivesArmor += 2; break;
                case "standard": body.GivesDmg += 0.12; break;
                case "focus": body.FirstDouble = true; break;
                case "lucky": critAdd += 0.18; break;
                case "brutal": critMulAdd += 0.30; break;
                case "cleanse": body.CleanseCard = true; break;
            }
        }

        static string Tags(Kit kit)
        {
            if (kit.Cards == null)
                return "";
            var line = "";
            for (var i = 0; i < kit.Cards.Length; i++)
            {
                var tag = LaneRoster.CardTag(kit.Cards[i]);
                if (string.IsNullOrEmpty(tag))
                    continue;
                if (line.Length > 0)
                    line += " · ";
                line += tag;
            }
            return line;
        }

        static MoveDef Copy(MoveDef src)
        {
            return new MoveDef
            {
                Id = src.Id, Name = src.Name, Shape = src.Shape, Aim = src.Aim,
                Cd = src.Cd, Range = src.Range, Dmg = src.Dmg, Radius = src.Radius,
                Cost = src.Cost, Drain = src.Drain, N = src.N, Lob = src.Lob,
                Strips = src.Strips, IgnoreArmor = src.IgnoreArmor, Execute = src.Execute,
                AtRange = src.AtRange, St = src.St, Self = src.Self
            };
        }

        static double Tune(string table, string id, double fallback)
        {
            var key = id + "=";
            var at = table.IndexOf(key, StringComparison.Ordinal);
            if (at < 0)
                return fallback;
            at += key.Length;
            var end = at;
            while (end < table.Length && table[end] != ',')
                end++;
            return double.Parse(table.Substring(at, end - at), System.Globalization.CultureInfo.InvariantCulture);
        }

        const string Crits = "kael=0.20,vale=0.15,orin=0.12,nia=0.22,lyra=0.16,arkos=0.18,d9ra=0.24,srath=0.20,kairos=0.14,justicae=0.17,seidon=0.15,sancora=0.13,lightfather=0.19,wraith=0.10,brute=0.08,imp=0.12,hurler=0.10,shade=0.16,thief=0.20,drain=0.12,gate=0.08,crown=0.10,smith=0.11,heartboss=0.09,levi=0.11,tithe=0.15,unnamer=0.13,lock=0.12,wolf=0.18,lion=0.19,bear=0.14,elephant=0.12,scorpion=0.20";
        const string Cds = "kael=0.86,vale=0.82,orin=0.78,nia=0.84,lyra=0.76,arkos=0.84,d9ra=0.88,srath=0.82,kairos=0.74,justicae=0.80,seidon=0.78,sancora=0.72,lightfather=0.82,wraith=0.88,brute=0.92,imp=0.86,hurler=0.90,shade=0.82,thief=0.80,drain=0.88,gate=0.94,crown=0.90,smith=0.90,heartboss=0.92,levi=0.90,tithe=0.86,unnamer=0.88,lock=0.94,wolf=0.84,lion=0.84,bear=0.88,elephant=0.90,scorpion=0.82";

        static Dictionary<string, MoveDef> BuildBook()
        {
            var map = new Dictionary<string, MoveDef>(96);
            Add(map, "rend|Rend|arc|enemy|7|2.1|1.35|bleed=10:5");
            Add(map, "shieldwall|Shield Wall|self|self|15|0|0|aegis=0.35,taunt=4");
            Add(map, "charge|Squire's Charge|blink|enemy|7|9|1.1|");
            Add(map, "cover|Cover|ally|allylow|11|9|0|aegis=0.34");
            Add(map, "bulwark|Bulwark|allies|allies|17|6|0|armor=3:8");
            Add(map, "unwrite|Unwrite|blast|enemy|8|8|1.5|r=2,strips");
            Add(map, "resonance|Resonance Flask|blast|clump|13|8|1.2|r=2.4,weak=0.25:6");
            Add(map, "dash|Dash|self|self|6|0|0|swift=0.35:3");
            Add(map, "pinvolley|Pinning Volley|multi|enemy|11|9|0.75|n=3,root=1.6");
            Add(map, "hymn|Hymn|aura|self|14|5.5|0.5|r=5.5,stun=1.3");
            Add(map, "chorus|Spiral Chorus|allies|allies|15|7|0|heal=0.18,haste=0.25:6");
            Add(map, "geodesic|Geodesic|lob|clump|10|10|1.45|r=2.2");
            Add(map, "trueline|True Line|beam|enemy|12|9|1.6|ignore");
            Add(map, "shockwave|Shockwave|aura|self|9|0|1.1|r=3.4,knock=2.4,stun=0.4");
            Add(map, "wolffury|Wolf Fury|self|self|18|0|0|rage=0.4:8,lifesteal=0.35:8");
            Add(map, "doublespeak|Doublespeak|self|self|11|0|0|veil=0.55:5,rage=0.8:3");
            Add(map, "needlerain|Needle Rain|multi|enemy|14|9|0.6|n=5");
            Add(map, "righttime|Right-Time|blast|clump|12|8|0.8|r=2.6,slow=0.45:5,self.swift=0.3:5");
            Add(map, "borrow|Borrowed Second|self|self|24|0|0|cdreset=1,haste=0.3:6");
            Add(map, "fairplate|Fair Plate|self|self|14|0|0|thorns=0.35:6");
            Add(map, "verdict|Verdict|melee|weakest|12|2.4|1.3|exec");
            Add(map, "current|Current|allies|allies|13|7|0|haste=0.3:7,swift=0.2:7");
            Add(map, "undertow|Undertow|aura|clump|15|7|0.7|r=3.2,pull=3.2,slow=0.4:4");
            Add(map, "chorusflask|Chorus Flask|allies|allies|13|7|0|heal=0.24");
            Add(map, "weavebloom|Weave Bloom|allies|allies|16|7|0|regen=6.5:6");
            Add(map, "seal9|Δ9 Seal|aura|self|20|6|0.9|r=6,stun=0.9,self.aegis=0.18,self.heal=0.06");
            Add(map, "provenance|Provenance|revive|deadally|40|30|0.30|");
            Add(map, "wail|Wail|aura|self|12|6|0.6|r=6,weak=0.3:6,root=0.9");
            Add(map, "pound|Ground Pound|aura|self|10|0|1.15|r=3,knock=2,stun=0.5");
            Add(map, "impfire|Imp Fire|bolt|enemy|8|8|1.2|burn=6:4,slow=0.4:4");
            Add(map, "lobstone|Lob Stone|lob|clump|9|11|1.4|r=2.2");
            Add(map, "flicker|Flicker|blink|enemy|8|8|0|stun=0.55,self.veil=0.6:4");
            Add(map, "snatch|Snatch|melee|enemy|9|2|1.3|drain=0.75,slow=0.35:3");
            Add(map, "unmake|Unmake|melee|weakest|13|2.2|1.55|silence=4,burn=7:5,root=1.1");
            Add(map, "gateslam|Gate Slam|aura|self|11|0|1.5|r=3.6,stun=1,knock=2.2");
            Add(map, "wardfield|Ward Field|self|self|16|0|0|aegis=0.4,thorns=0.4:6");
            Add(map, "cryptward|Crypt Ward|self|self|18|0|0|aegis=0.22,thorns=0.25:5");
            Add(map, "gateward|Gate Ward|self|self|18|0|0|aegis=0.16,thorns=0.2:5");
            Add(map, "callwards|Call the Wards|summon|self|22|0|0|n=2");
            Add(map, "regalvolley|Regal Volley|multi|clump|10|10|0.9|n=3,lob,r=1.4");
            Add(map, "anvil|Anvil|blast|enemy|11|2.4|1.4|r=2,burn=8:5");
            Add(map, "standard|Standard Raised|allies|allies|16|6|0|heal=0.15,haste=0.2:5");
            Add(map, "temperwork|Temper Work|allies|allies|17|6|0|heal=0.16,haste=0.15:4");
            Add(map, "heartbeat|Heartbeat|allies|allies|12|6|0|heal=0.20");
            Add(map, "tidecrush|Tide Crush|aura|clump|14|6|1.2|r=3.4,pull=3.6,slow=0.35:4");
            Add(map, "tithe|Tithe|melee|enemy|12|2.2|1.2|drain=0.6,weak=0.22:5");
            Add(map, "nameless|Nameless|blast|clump|15|8|1.3|r=2.8,silence=5");
            Add(map, "lockseal|The Lock Seals|aura|self|18|0|1.35|r=4.4,stun=1.2,burn=9:5");
            Add(map, "gatecannon|Gate Cannon|beam|enemy|12|10|1.5|");
            Add(map, "bite|Pack Bite|blink|weakest|7|8|1.2|root=1.2");
            Add(map, "roar|Roar|aura|self|12|5.5|0.8|r=5.5,weak=0.25:5,knock=1.8");
            Add(map, "soljump|Solstride|blink|weakest|8|10|1.25|");
            Add(map, "maul|Maul|aura|self|11|0|1.2|r=2.8,knock=1.4");
            Add(map, "stomp|Stomp|aura|self|12|0|1.1|r=3.2,stun=0.8");
            Add(map, "clamp|Glassbarb Clamp|melee|enemy|9|2|1.3|burn=5:4");
            Add(map, "stride|Stride|stride|enemy|30|7|0|cost=8");
            Add(map, "gate_haul|Gate Haul|blast|enemy|8|6|0.6|pull=3.1,slow=0.35:3");
            Add(map, "decree|Decree|blast|enemy|8|6.5|0.6|pull=3.1,slow=0.35:3");
            Add(map, "maulgrip|Maul Grip|blast|enemy|8|5.5|0.6|pull=3.1,slow=0.35:3");
            Add(map, "shieldhook|Shield Hook|blast|enemy|8|5.5|0.6|pull=3.1,slow=0.35:3");
            Add(map, "irongrip|Iron Grip|blast|enemy|8|5.5|0.6|pull=3.1,slow=0.35:3");
            Add(map, "gravstep|Grave Step|blink|weakest|7|8|1.15|slow=0.3:2.5");
            Add(map, "collection|Collection|blink|weakest|7|8|1.15|slow=0.3:2.5");
            Add(map, "tuskcharge|Tusk Charge|blink|enemy|7|9|1.15|slow=0.3:2.5");
            Add(map, "cryptsurge|Crypt Surge|blink|enemy|7|8|1.15|slow=0.3:2.5");
            Add(map, "writpursuit|Writ of Pursuit|blink|weakest|7|9|1.15|slow=0.3:2.5");
            return map;
        }

        static void Add(Dictionary<string, MoveDef> map, string row)
        {
            var p = row.Split('|');
            var mv = new MoveDef
            {
                Id = p[0],
                Name = p[1],
                Shape = p[2],
                Aim = p[3],
                Cd = double.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture),
                Range = double.Parse(p[5], System.Globalization.CultureInfo.InvariantCulture),
                Dmg = double.Parse(p[6], System.Globalization.CultureInfo.InvariantCulture)
            };
            if (p.Length > 7 && p[7].Length > 0)
            {
                var bits = p[7].Split(',');
                for (var i = 0; i < bits.Length; i++)
                    Stamp(mv, bits[i]);
            }
            map[mv.Id] = mv;
        }

        static void Stamp(MoveDef mv, string bit)
        {
            var self = bit.StartsWith("self.", StringComparison.Ordinal);
            if (self)
                bit = bit.Substring(5);
            if (bit == "strips") { mv.Strips = true; return; }
            if (bit == "ignore") { mv.IgnoreArmor = true; return; }
            if (bit == "exec") { mv.Execute = true; return; }
            if (bit == "lob") { mv.Lob = true; return; }
            var eq = bit.IndexOf('=');
            if (eq < 0)
                return;
            var key = bit.Substring(0, eq);
            var val = bit.Substring(eq + 1);
            if (key == "r") { mv.Radius = D(val); return; }
            if (key == "n") { mv.N = (int)D(val); return; }
            if (key == "cost") { mv.Cost = D(val); return; }
            if (key == "drain") { mv.Drain = D(val); return; }
            var st = self ? (mv.Self ?? (mv.Self = new Status())) : (mv.St ?? (mv.St = new Status()));
            var pair = val.Split(':');
            var a = D(pair[0]);
            var b = pair.Length > 1 ? D(pair[1]) : 0;
            switch (key)
            {
                case "stun": st.Stun = a; break;
                case "root": st.Root = a; break;
                case "silence": st.Silence = a; break;
                case "knock": st.Knock = a; break;
                case "pull": st.Pull = a; break;
                case "aegis": st.Aegis = a; break;
                case "taunt": st.Taunt = a; break;
                case "heal": st.Heal = a; break;
                case "cdreset": st.CdReset = a; break;
                case "slow": st.SlowV = a; st.SlowD = b; break;
                case "swift": st.SwiftV = a; st.SwiftD = b; break;
                case "haste": st.HasteV = a; st.HasteD = b; break;
                case "rage": st.RageV = a; st.RageD = b; break;
                case "weak": st.WeakV = a; st.WeakD = b; break;
                case "burn": st.BurnD = a; st.BurnT = b; break;
                case "bleed": st.BleedD = a; st.BleedT = b; break;
                case "regen": st.RegenD = a; st.RegenT = b; break;
                case "thorns": st.ThornV = a; st.ThornT = b; break;
                case "wound": st.WoundV = a; st.WoundD = b; break;
                case "armor": st.Armor = a; st.ArmorT = b; break;
                case "veil": st.VeilV = a; st.VeilD = b; break;
                case "lifesteal": st.LifeV = a; st.LifeD = b; break;
            }
        }

        static double D(string text)
        {
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
