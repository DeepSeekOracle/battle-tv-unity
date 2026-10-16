using System;
using System.Collections.Generic;
using BattleTv.Menu;

namespace BattleTv.Fight
{
    public sealed class ArenaBody
    {
        public string Id;
        public string Name;
        public string Atk;
        public int Team;
        public int Slot;
        public double X;
        public double Y;
        public double Hp;
        public double Max;
        public double Dmg;
        public double Rate;
        public double Speed;
        public double Armor;
        public double Range;
        public double AtkCd;
        public double Swing;
        public double Mana;
        public double ManaMax;
        public double ManaRegen;
        public double Shield;
        public double StrideCd;
        public string Cards = "";
        public string Callout = "";
        public bool Alive = true;
    }

    public sealed class ArenaBolt
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public int Team;
        public double Dmg;
        public ArenaBody Target;
    }

    /// <summary>
    /// The swept sand. Outer stone only, the same law as the live ring while COVER is off.
    /// Bodies use the book line. A kit's named moves arrive with that character.
    /// The side lane in SideDuel is not this fight.
    /// </summary>
    public static class ArenaMatch
    {
        public const int MapW = 34;
        public const int MapH = 22;
        public const int Tile = 32;
        public const double WorldW = MapW * Tile;
        public const double WorldH = MapH * Tile;
        public const double Inset = Tile * 1.15;
        public const double Sep = 40;
        const double GunTiles = 110.0 / 32.0;

        public static readonly List<ArenaBody> Bodies = new List<ArenaBody>(10);
        public static readonly List<ArenaBolt> Bolts = new List<ArenaBolt>(16);
        public static double Time;
        public static bool Over;
        public static string Title = "";
        public static string Detail = "";

        public static void Begin(IList<string> yours, IList<string> foes, int seed)
        {
            Bodies.Clear();
            Bolts.Clear();
            Time = 0;
            Over = false;
            Title = "";
            Detail = "";
            Place(yours, 0);
            Place(foes, 1);
        }

        public static void Step(double dt)
        {
            if (Over || dt <= 0)
                return;
            if (dt > 0.05)
                dt = 0.05;
            Time += dt;
            for (var i = Bolts.Count - 1; i >= 0; i--)
            {
                var bolt = Bolts[i];
                var aim = bolt.Target;
                if (aim != null && aim.Alive)
                {
                    var dx = aim.X - bolt.X;
                    var dy = aim.Y - bolt.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 18)
                    {
                        Land(bolt.Dmg, aim);
                        Bolts.RemoveAt(i);
                        continue;
                    }
                    var step = 14 * Tile * dt;
                    bolt.X += dx / dist * step;
                    bolt.Y += dy / dist * step;
                }
                else
                {
                    bolt.X += bolt.Vx * dt;
                    bolt.Y += bolt.Vy * dt;
                    if (bolt.X < 0 || bolt.Y < 0 || bolt.X > WorldW || bolt.Y > WorldH)
                        Bolts.RemoveAt(i);
                }
            }
            for (var i = 0; i < Bodies.Count; i++)
            {
                var body = Bodies[i];
                if (!body.Alive)
                    continue;
                body.Swing = Math.Max(0, body.Swing - dt);
                body.AtkCd = Math.Max(0, body.AtkCd - dt);
                body.StrideCd = Math.Max(0, body.StrideCd - dt);
                var foe = Nearest(body);
                if (foe == null)
                {
                    body.Mana = Math.Min(body.ManaMax, body.Mana + body.ManaRegen * dt);
                    continue;
                }
                var dx = foe.X - body.X;
                var dy = foe.Y - body.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 0.001)
                    dist = 0.001;
                var stop = Math.Max(Tile * 0.9, body.Range * Tile);
                if (body.StrideCd <= 0 && body.Mana >= SideDuel.CloseCost && dist > stop + Tile * 2.2)
                {
                    var gap = dist - (stop + 18);
                    body.X += dx / dist * gap;
                    body.Y += dy / dist * gap;
                    body.Mana -= SideDuel.CloseCost;
                    body.StrideCd = SideDuel.CloseCd;
                    body.Callout = "Close";
                    body.Swing = 0.28;
                    Hold(body);
                    dx = foe.X - body.X;
                    dy = foe.Y - body.Y;
                    dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 0.001)
                        dist = 0.001;
                }
                body.Mana = Math.Min(body.ManaMax, body.Mana + body.ManaRegen * dt);
                if (dist > stop)
                {
                    var step = body.Speed * Tile * dt;
                    if (step > dist - stop)
                        step = dist - stop;
                    body.X += dx / dist * step;
                    body.Y += dy / dist * step;
                }
                Hold(body);
                if (dist <= stop + 10 && body.AtkCd <= 0)
                {
                    body.AtkCd = 1.0 / Math.Max(0.25, body.Rate);
                    body.Swing = 0.28;
                    body.Callout = body.Atk;
                    var amount = body.Dmg * SideDuel.SuddenScale(Time);
                    if (body.Range > GunTiles)
                    {
                        Bolts.Add(new ArenaBolt
                        {
                            X = body.X,
                            Y = body.Y,
                            Vx = dx / dist * 14 * Tile,
                            Vy = dy / dist * 14 * Tile,
                            Team = body.Team,
                            Dmg = amount,
                            Target = foe
                        });
                    }
                    else
                        Land(amount, foe);
                }
            }
            for (var n = 0; n < 3; n++)
                Ease();
            if (Time >= SideDuel.HardStop)
                Close("Neither side fell.", "The sand is a draw.");
            else if (SideDown(0) && SideDown(1))
                Close("Neither side stands.", "The sand took both.");
            else if (SideDown(1))
                Close("Your side takes the sand.", Names(0));
            else if (SideDown(0))
                Close("The blind side takes the sand.", Names(1));
        }

        static void Place(IList<string> ids, int team)
        {
            if (ids == null)
                return;
            var slots = Slots(ids.Count);
            for (var i = 0; i < ids.Count && i < slots.Length; i++)
            {
                var kit = LaneRoster.ById(ids[i]);
                if (kit.Id == null)
                    continue;
                var slot = slots[i];
                var body = new ArenaBody
                {
                    Id = kit.Id,
                    Name = kit.Name,
                    Atk = string.IsNullOrEmpty(kit.Atk) ? "Strike" : kit.Atk,
                    Team = team,
                    Slot = slot,
                    X = (team == 0 ? 3 : MapW - 4) * Tile,
                    Y = (3 + slot * 4) * Tile,
                    Hp = Math.Max(1, kit.Hp),
                    Max = Math.Max(1, kit.Hp),
                    Dmg = kit.Dmg,
                    Rate = kit.Rate,
                    Speed = Math.Max(0.4, kit.Speed),
                    Armor = kit.Armor,
                    Range = Math.Max(0.8, kit.Range),
                    Mana = kit.Mana,
                    ManaMax = kit.Mana,
                    ManaRegen = LaneRoster.ManaRegen(kit.Id),
                    Shield = 0,
                    StrideCd = 0,
                    Cards = Tags(kit)
                };
                Bodies.Add(body);
            }
        }

        static string Tags(Kit kit)
        {
            if (kit.Cards == null || kit.Cards.Length == 0)
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

        static int[] Slots(int count)
        {
            if (count <= 1)
                return new[] { 2 };
            if (count == 2)
                return new[] { 1, 3 };
            if (count == 3)
                return new[] { 1, 2, 3 };
            if (count == 4)
                return new[] { 0, 1, 3, 4 };
            return new[] { 0, 1, 2, 3, 4 };
        }

        static ArenaBody Nearest(ArenaBody body)
        {
            ArenaBody best = null;
            var bestD = double.MaxValue;
            for (var i = 0; i < Bodies.Count; i++)
            {
                var other = Bodies[i];
                if (!other.Alive || other.Team == body.Team)
                    continue;
                var dx = other.X - body.X;
                var dy = other.Y - body.Y;
                var d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    bestD = d;
                    best = other;
                }
            }
            return best;
        }

        static void Land(double amount, ArenaBody target)
        {
            if (target == null || !target.Alive)
                return;
            var dmg = amount - target.Armor * 1.1;
            if (dmg < 0.5)
                return;
            target.Hp -= dmg;
            if (target.Hp <= 0)
            {
                target.Hp = 0;
                target.Alive = false;
                target.Callout = "";
            }
        }

        static void Ease()
        {
            for (var i = 0; i < Bodies.Count; i++)
            {
                var a = Bodies[i];
                if (!a.Alive)
                    continue;
                for (var j = i + 1; j < Bodies.Count; j++)
                {
                    var b = Bodies[j];
                    if (!b.Alive)
                        continue;
                    var dx = b.X - a.X;
                    var dy = b.Y - a.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist >= Sep || dist < 0.001)
                        continue;
                    var push = (Sep - dist) * 0.5;
                    a.X -= dx / dist * push;
                    a.Y -= dy / dist * push;
                    b.X += dx / dist * push;
                    b.Y += dy / dist * push;
                    Hold(a);
                    Hold(b);
                }
            }
        }

        static void Hold(ArenaBody body)
        {
            if (body.X < Inset)
                body.X = Inset;
            if (body.Y < Inset)
                body.Y = Inset;
            if (body.X > WorldW - Inset)
                body.X = WorldW - Inset;
            if (body.Y > WorldH - Inset)
                body.Y = WorldH - Inset;
        }

        static bool SideDown(int team)
        {
            var any = false;
            for (var i = 0; i < Bodies.Count; i++)
            {
                if (Bodies[i].Team != team)
                    continue;
                any = true;
                if (Bodies[i].Alive)
                    return false;
            }
            return any;
        }

        static string Names(int team)
        {
            var line = "";
            for (var i = 0; i < Bodies.Count; i++)
            {
                if (Bodies[i].Team != team)
                    continue;
                if (line.Length > 0)
                    line += "  ·  ";
                line += Bodies[i].Name;
            }
            return line;
        }

        static void Close(string title, string detail)
        {
            Over = true;
            Title = title;
            Detail = detail;
        }
    }
}
