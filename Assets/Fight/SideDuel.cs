using System;
using System.Collections.Generic;

namespace BattleTv.Fight
{
    /// <summary>
    /// One-lane 1v1 using Vale and Brute's roster sheet. Numbers are the live
    /// build() for a solo body: melee health ×1.18, damage ×1.15, armor +1.
    /// A second shield does not stack. Armor may stop a blow completely.
    /// </summary>
    public sealed class Body
    {
        public string Name;
        public string Cards;
        public double Hp;
        public double Max;
        public double Shield;
        public double Dmg;
        public double Armor;
        public double Rate;
        public double Speed;
        public double Range;
        public double Crit;
        public double CritMul;
        public double CdMul;
        public double Bulwark;
        public double Thorns;
        public double ThornsT;
        public double ThornsV;
        public double Mana;
        public double ManaMax;
        public double ManaRegen;
        public double X;
        public double AtkCd;
        public double WallCd;
        public double PoundCd;
        public double WardCd;
        public double StrideCd;
        public double Stun;
        public double Swing;
        public double CoverCd;
        public double BulwarkCd;
        public double ArmorBuff;
        public double ArmorBuffT;
        public double SlowT;
        public double SlowMul;
        public double GripCd;
        public bool Wall;
        public bool Pound;
        public bool Ward;
        public string Callout = "";
        public string Plate = "";

        public bool Alive => Hp > 0;
    }

    /// <summary>
    /// A hit the lane can show. The numbers are unchanged. This only records
    /// what already happened so the view can put a flash and a figure on it.
    /// </summary>
    public struct Blow
    {
        public string Text;
        public string Tone;
        public string Target;
        public double X;
    }

    public static class SideDuel
    {
        public const double Lane = 18;
        // The foot point sits in from the plate edge so the painted body
        // stays on the sand, against the side stones.
        public const double Wall = 2.8;
        public const double CloseCd = 30;
        public const double CloseCost = 8;
        public const double ValeCharge = 1.15;
        // One body cannot chew a full plate before its cooldown is ready.
        // The lane pace is applied to the blow that reaches Hit. The card
        // test still calls Hit with the raw amount, so 14 and 0 stay put.
        public const double LanePace = 1.8;
        public static double DoorX;
        public static double DoorT;
        static readonly List<Blow> Pending = new List<Blow>(8);

        public static void TakeBlows(List<Blow> into)
        {
            if (into == null)
                return;
            for (int i = 0; i < Pending.Count; i++)
                into.Add(Pending[i]);
            Pending.Clear();
        }

        static void Note(Body tgt, string text, string tone)
        {
            if (tgt == null)
                return;
            if (Pending.Count > 16)
                Pending.RemoveAt(0);
            Pending.Add(new Blow { Text = text, Tone = tone, Target = tgt.Name, X = tgt.X });
        }
        const double ReachPad = 0.5;
        const double CoverCost = 12.1;
        const double BulwarkCost = 18.7;
        const double DoorHalf = 0.48;
        const double WallCost = 16.5;
        const double WardCost = 17.6;
        const double PoundCost = 11;
        const double GripCost = 12;
        const double GunTiles = 110.0 / 32.0;

        public static Body Vale()
        {
            var body = Make("Vale", "Door Flesh · Bumper Thorn · Fair Plate",
                895, 26, 0.72, 3.3, 8, 1.6, 0.15, 0.82, 0, 0, 3, 0.22, 0.30, 2.4);
            body.Wall = true;
            body.ManaMax = body.Mana = 60;
            body.ManaRegen = 4;
            return body;
        }

        public static Body Brute()
        {
            var body = Make("Brute", "Fair Plate · Deeper Well · Bumper Thorn",
                760, 34, 0.66, 2.0, 5, 1.5, 0.08, 0.92, 0.40, -0.12, 3, 0, 0.30, 15.6);
            body.Pound = true;
            body.Ward = true;
            body.ManaMax = body.Mana = 40;
            body.ManaRegen = 3;
            return body;
        }

        public static bool RaiseWall(Body self)
        {
            if (self.Mana < WallCost)
                return false;
            self.Mana -= WallCost;
            if (!(self.Shield > 0))
            {
                self.Shield += Math.Round(self.Max * 0.35);
                self.Plate = "wall";
            }
            self.WallCd = 15 * self.CdMul;
            self.Callout = "Shield Wall";
            self.Swing = 0.2;
            return true;
        }

        /// <summary>
        /// Close is a thirty-second step. Vale uses it only onto a gun that is
        /// still outside her swing. Brute uses it onto whoever is in front of
        /// him. Either fighter, pinned on the side stones with the other in
        /// their face, leaps past them. That way out does not wait on the gun
        /// rule. The clock ignores cdMul. It does not cross the planted kite.
        /// </summary>
        public static bool TryClose(Body self, Body other)
        {
            if (self == null || other == null || !self.Alive || !other.Alive)
                return false;
            if (self.StrideCd > 0 || self.Mana < CloseCost)
                return false;
            if (DoorBlocks(self.X, other.X))
                return false;
            double dist = Math.Abs(self.X - other.X);
            // Checked before the gun rule. Vale versus Brute never passed that
            // rule, so she stood on the stones and never took the leap.
            if (Pinned(self) && dist <= self.Range + 1.5 && LeapPast(self, other))
                return true;
            bool ontoGun = self.Range <= GunTiles && other.Range > GunTiles;
            bool bruteStep = self.Name == "Brute" && self.Range <= GunTiles;
            if (!ontoGun && !bruteStep)
                return false;
            double reach = 7 + self.Range;
            if (dist <= self.Range + 1.5 || dist > reach)
                return false;
            double dir = Math.Sign(other.X - self.X);
            if (dir == 0)
                dir = 1;
            self.X = other.X - dir * (self.Range + 0.25);
            self.Mana -= CloseCost;
            self.StrideCd = CloseCd;
            self.Callout = "Close";
            self.Swing = 0.4;
            return true;
        }

        static bool Pinned(Body self)
        {
            return self.X <= Wall + 0.45 || self.X >= Lane - Wall - 0.45;
        }

        static bool LeapPast(Body self, Body other)
        {
            bool onLeft = self.X <= Wall + 0.45;
            if (onLeft && other.X < self.X - 0.05)
                return false;
            if (!onLeft && other.X > self.X + 0.05)
                return false;
            double side = Math.Sign(self.X - other.X);
            if (side == 0)
                side = onLeft ? -1 : 1;
            double land = other.X - side * (Math.Max(self.Range, other.Range) + 0.85);
            if (DoorBlocks(self.X, land) || DoorBlocks(land, other.X))
                return false;
            land = Clamp(land);
            double crossed = Math.Sign(land - other.X);
            if (crossed == 0 || crossed == side)
                return false;
            self.X = land;
            self.Mana -= CloseCost;
            self.StrideCd = CloseCd;
            self.Callout = "Close";
            self.Swing = 0.4;
            return true;
        }

        public const double SuddenAt = 180;
        public const double HardStop = 600;
        const double CastBand = 3;

        public static double SuddenScale(double time)
        {
            if (time < SuddenAt)
                return 1;
            double mins = Math.Floor((time - SuddenAt) / 60) + 1;
            return 1 + Math.Min(4, mins) * 0.25;
        }

        public static int Hit(Body src, Body tgt, double amount, bool melee, bool crit, bool reflect, double time = 0)
        {
            if (tgt == null || tgt.Hp <= 0)
                return 0;

            double dmg = amount;
            if (crit && src != null)
                dmg *= src.CritMul;
            if (tgt.Bulwark > 0 && tgt.Hp / tgt.Max > 0.6)
                dmg *= 1 - tgt.Bulwark;
            if (!reflect)
                dmg *= SuddenScale(time);
            if (!reflect)
            {
                double armor = tgt.Armor + (tgt.ArmorBuffT > 0 ? tgt.ArmorBuff : 0);
                dmg -= armor * 1.1;
                if (dmg < 0.5)
                {
                    Note(tgt, "block", "block");
                    return 0;
                }
            }

            // Bumper Thorn takes thirty percent off a melee blow. It does not
            // answer. The ward's timed thorns still answer, and they do not add
            // the card on top of that answer.
            if (melee && !reflect && tgt.Thorns > 0)
                dmg *= 1 - tgt.Thorns;

            bool broke = false;
            if (tgt.Shield > 0)
            {
                double absorbed = Math.Min(tgt.Shield, dmg);
                tgt.Shield -= absorbed;
                dmg -= absorbed;
                broke = tgt.Shield <= 0;
                if (dmg < 0.5)
                {
                    if (broke)
                        OpenPlate(tgt);
                    Note(tgt, "ward", "ward");
                    return 0;
                }
            }

            dmg = Math.Round(dmg);
            if (dmg <= 0)
            {
                if (broke)
                    OpenPlate(tgt);
                return 0;
            }
            if (broke)
                OpenPlate(tgt);
            tgt.Hp -= dmg;
            if (tgt.Hp < 0)
                tgt.Hp = 0;
            tgt.Swing = 0.12;
            bool killed = tgt.Hp <= 0;
            Note(tgt, ((int)dmg) + (crit ? "!" : ""), crit ? "crit" : "hit");
            if (killed)
                Note(tgt, "DOWN", "down");

            if (melee && !reflect && src != null && tgt.ThornsT > 0)
            {
                double back = dmg * tgt.ThornsV;
                if (back > 1)
                    Hit(tgt, src, back, false, false, true);
            }

            return (int)dmg;
        }

        public static void Step(Body a, Body b, double dt, Random rng, double time)
        {
            if (dt <= 0 || a == null || b == null)
                return;
            if (DoorT > 0)
                DoorT = Math.Max(0, DoorT - dt);
            double ax = a.X;
            double bx = b.X;
            Tick(a, dt);
            Tick(b, dt);
            Act(a, b, dt, rng, time);
            Act(b, a, dt, rng, time);
            a.X = Clamp(StopCrossing(ax, a.X));
            b.X = Clamp(StopCrossing(bx, b.X));
        }

        static void Tick(Body body, double dt)
        {
            body.AtkCd = Math.Max(0, body.AtkCd - dt);
            body.WallCd = Math.Max(0, body.WallCd - dt);
            body.PoundCd = Math.Max(0, body.PoundCd - dt);
            body.WardCd = Math.Max(0, body.WardCd - dt);
            body.Stun = Math.Max(0, body.Stun - dt);
            body.ThornsT = Math.Max(0, body.ThornsT - dt);
            body.Swing = Math.Max(0, body.Swing - dt);
            body.StrideCd = Math.Max(0, body.StrideCd - dt);
            body.GripCd = Math.Max(0, body.GripCd - dt);
            body.SlowT = Math.Max(0, body.SlowT - dt);
            body.CoverCd = Math.Max(0, body.CoverCd - dt);
            body.BulwarkCd = Math.Max(0, body.BulwarkCd - dt);
            body.ArmorBuffT = Math.Max(0, body.ArmorBuffT - dt);
            body.Mana = Math.Min(body.ManaMax, body.Mana + body.ManaRegen * dt);
        }

        static void Act(Body self, Body other, double dt, Random rng, double time)
        {
            if (!self.Alive || !other.Alive || self.Stun > 0)
                return;

            bool stepped = TryClose(self, other);
            double dist = Math.Abs(self.X - other.X);
            bool close = dist < CastBand;
            if (!stepped && self.Pound && self.PoundCd <= 0 && close && self.Mana >= PoundCost && !DoorBlocks(self.X, other.X))
            {
                self.Mana -= PoundCost;
                bool crit = rng.NextDouble() < self.Crit;
                Hit(self, other, self.Dmg * 1.15 * LanePace, true, crit, false, time);
                other.Stun = Math.Max(other.Stun, 0.5);
                double dir = Math.Sign(other.X - self.X);
                if (dir == 0)
                    dir = 1;
                other.X += dir * 2;
                self.PoundCd = 10 * self.CdMul;
                self.Callout = "Brute Pound";
                self.Swing = 0.7;
            }
            else if (!stepped && self.Ward && self.WardCd <= 0 && close && self.Mana >= WardCost && !(self.Shield > 0))
            {
                self.Mana -= WardCost;
                self.Shield += Math.Round(self.Max * 0.4);
                self.Plate = "ward";
                self.ThornsV = Math.Max(self.ThornsV, 0.4);
                self.ThornsT = Math.Max(self.ThornsT, 6);
                self.WardCd = 16 * self.CdMul;
                self.Callout = "Brute Ward";
                self.Swing = 0.45;
            }
            else if (!stepped && TryGrip(self, other, dist))
            {
                // In the gap the grip is the action. In melee the club still lands.
                if (dist > self.Range + ReachPad)
                    return;
            }
            else if (!stepped && self.Name == "Vale" && ThinkVale(self, other))
                return;
            else if (!stepped && self.Name != "Vale" && self.Wall && self.WallCd <= 0 && close)
                RaiseWall(self);

            dist = Math.Abs(self.X - other.X);
            if (DoorBlocks(self.X, other.X))
            {
                // The planted kite is solid. She steps off it. He does not swing through it.
                if (self.Name == "Vale")
                {
                    double away = Math.Sign(self.X - other.X);
                    if (away == 0)
                        away = -1;
                    self.X += away * Pace(self) * 0.7 * dt;
                }
                else if (self.AtkCd <= 0 && dist <= self.Range + ReachPad + 0.4)
                {
                    self.AtkCd = 0.55;
                    self.Swing = 0.16;
                }
                return;
            }
            double reach = self.Range + ReachPad;
            if (self.Name == "Vale" && dist <= reach + ValeCharge)
            {
                if (self.AtkCd > 0)
                {
                    if (dist > reach)
                        self.X += Math.Sign(other.X - self.X) * Pace(self) * dt;
                    return;
                }
                bool bash = dist > reach;
                if (bash)
                {
                    double dir = Math.Sign(other.X - self.X);
                    if (dir == 0)
                        dir = 1;
                    self.X += dir * Math.Min(ValeCharge, dist - reach + 0.08);
                    dist = Math.Abs(self.X - other.X);
                }
                if (dist <= reach + 0.12)
                {
                    bool crit = rng.NextDouble() < self.Crit;
                    Hit(self, other, self.Dmg * LanePace, true, crit, false, time);
                }
                self.AtkCd = 1.0 / Math.Max(0.15, self.Rate);
                self.Callout = bash ? "Shield Bash" : "Punch";
                self.Swing = bash ? 0.36 : 0.24;
                return;
            }
            if (self.Name != "Vale" && !stepped && dist <= reach)
            {
                if (self.AtkCd > 0)
                    return;
                bool crit = rng.NextDouble() < self.Crit;
                Hit(self, other, self.Dmg * LanePace, true, crit, false, time);
                self.AtkCd = 1.0 / Math.Max(0.15, self.Rate);
                self.Callout = "Heavy Sweep";
                self.Swing = 1.2;
                return;
            }

            double step = Math.Sign(other.X - self.X) * Pace(self) * dt;
            self.X += step;
        }

        public static void ResetLane()
        {
            DoorT = 0;
            DoorX = 0;
        }

        /// <summary>
        /// Vale's kit, in the live order: Cover, Line Bulwark, then Aegis Wall.
        /// The wall is the solid shield in the ground. It is not a second plate.
        /// </summary>
        static bool ThinkVale(Body self, Body other)
        {
            double dist = Math.Abs(self.X - other.X);
            if (self.CoverCd <= 0 && self.Mana >= CoverCost && self.Shield <= 0 && self.Hp / self.Max < 0.8)
            {
                self.Mana -= CoverCost;
                self.Shield += Math.Round(self.Max * 0.34);
                self.Plate = "cover";
                self.CoverCd = 11 * self.CdMul;
                self.Callout = "Aegis Cover";
                self.Swing = 0.4;
                return true;
            }
            if (self.BulwarkCd <= 0 && self.Mana >= BulwarkCost && self.ArmorBuffT <= 0 && (self.Hp / self.Max < 0.92 || dist < 5))
            {
                self.Mana -= BulwarkCost;
                self.ArmorBuff = 3;
                self.ArmorBuffT = 8;
                self.BulwarkCd = 17 * self.CdMul;
                self.Callout = "Line Bulwark";
                self.Swing = 0.42;
                return true;
            }
            if (self.WallCd <= 0 && DoorT <= 0 && self.Mana >= WallCost && dist < 2.8)
            {
                self.Mana -= WallCost;
                if (!(self.Shield > 0))
                {
                    self.Shield += Math.Round(self.Max * 0.35);
                    self.Plate = "wall";
                }
                self.WallCd = 30;
                DoorX = (self.X + other.X) * 0.5;
                DoorT = 3;
                NudgeOffDoor(self);
                NudgeOffDoor(other);
                self.Callout = "Aegis Wall";
                self.Swing = 0.45;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Iron Grip is Brute's slow. Rocks sit on their feet for the duration.
        /// It reaches just past his club, and it does not cross the planted kite.
        /// </summary>
        static bool TryGrip(Body self, Body other, double dist)
        {
            if (self.Name != "Brute" || self.GripCd > 0 || self.Mana < GripCost)
                return false;
            if (dist < 0.4 || dist > self.Range + 3.2)
                return false;
            if (DoorBlocks(self.X, other.X))
                return false;
            self.Mana -= GripCost;
            self.GripCd = 14 * self.CdMul;
            other.SlowT = Math.Max(other.SlowT, 4);
            other.SlowMul = 0.42;
            self.Callout = "Iron Grip";
            self.Swing = 0.35;
            return true;
        }

        /// <summary>
        /// A new plate waits out its cooldown after the old one breaks.
        /// Running that clock while the plate is up let the next one land
        /// on the same tick the first one failed.
        /// </summary>
        static void OpenPlate(Body body)
        {
            if (body.Plate == "ward")
                body.WardCd = 16 * body.CdMul;
            else if (body.Plate == "cover")
                body.CoverCd = 11 * body.CdMul;
            else if (body.Plate == "wall" && body.Name == "Vale")
                body.WallCd = 30;
            else if (body.Plate == "wall")
                body.WallCd = 15 * body.CdMul;
            body.Plate = "";
        }

        static double Pace(Body body)
        {
            if (body.SlowT > 0 && body.SlowMul > 0)
                return body.Speed * (1 - body.SlowMul);
            return body.Speed;
        }

        static bool DoorBlocks(double a, double b)
        {
            if (DoorT <= 0)
                return false;
            return (a - DoorX) * (b - DoorX) < 0;
        }

        static double StopCrossing(double from, double to)
        {
            if (DoorT <= 0)
                return to;
            if ((from - DoorX) * (to - DoorX) >= 0)
                return to;
            double side = Math.Sign(from - DoorX);
            if (side == 0)
                side = -1;
            return DoorX + side * (DoorHalf + 0.06);
        }

        static void NudgeOffDoor(Body body)
        {
            double gap = body.X - DoorX;
            if (Math.Abs(gap) >= DoorHalf + 0.08)
                return;
            double side = gap == 0 ? -1 : Math.Sign(gap);
            body.X = DoorX + side * (DoorHalf + 0.08);
        }

        static double Clamp(double x)
        {
            if (x < Wall)
                return Wall;
            if (x > Lane - Wall)
                return Lane - Wall;
            return x;
        }

        static Body Make(string name, string cards, double hp, double dmg, double rate, double speed, double armor, double range, double crit, double cdMul, double hpMul, double spdMul, double armorAdd, double bulwark, double thorns, double x)
        {
            bool melee = range <= 1.95;
            double max = Math.Max(120, Math.Round(hp * (1 + hpMul) * (melee ? 1.18 : 1)));
            return new Body
            {
                Name = name,
                Cards = cards,
                Max = max,
                Hp = max,
                Dmg = dmg * 1.15,
                Rate = rate,
                Speed = speed * (1 + spdMul),
                Armor = armor + armorAdd + (melee ? 1 : 0),
                Range = range,
                Crit = crit,
                CritMul = 1.5,
                CdMul = cdMul,
                Bulwark = bulwark,
                Thorns = thorns,
                X = x,
                AtkCd = 0.35
            };
        }
    }
}
