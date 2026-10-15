using System;

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
        public bool Wall;
        public bool Pound;
        public bool Ward;
        public string Callout = "";

        public bool Alive => Hp > 0;
    }

    public static class SideDuel
    {
        public const double Lane = 18;
        public const double CloseCd = 30;
        public const double CloseCost = 8;
        const double ReachPad = 0.5;
        const double WallCost = 16.5;
        const double WardCost = 17.6;
        const double PoundCost = 11;
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
                self.Shield += Math.Round(self.Max * 0.35);
            self.WallCd = 15 * self.CdMul;
            self.Callout = "Shield Wall";
            self.Swing = 0.2;
            return true;
        }

        /// <summary>
        /// Close is the fist's thirty-second step, and only onto a gun that is
        /// still outside the swing. Two fists walk. The clock ignores cdMul.
        /// </summary>
        public static bool TryClose(Body self, Body other)
        {
            if (self == null || other == null || !self.Alive || !other.Alive)
                return false;
            if (self.StrideCd > 0 || self.Mana < CloseCost)
                return false;
            if (self.Range > GunTiles || other.Range <= GunTiles)
                return false;
            double dist = Math.Abs(self.X - other.X);
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
            self.Swing = 0.2;
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
                dmg -= tgt.Armor * 1.1;
                if (dmg < 0.5)
                {
                    tgt.Callout = "block";
                    return 0;
                }
            }

            if (tgt.Shield > 0)
            {
                double absorbed = Math.Min(tgt.Shield, dmg);
                tgt.Shield -= absorbed;
                dmg -= absorbed;
                if (dmg < 0.5)
                {
                    tgt.Callout = "ward";
                    return 0;
                }
            }

            dmg = Math.Round(dmg);
            if (dmg <= 0)
                return 0;
            tgt.Hp -= dmg;
            if (tgt.Hp < 0)
                tgt.Hp = 0;
            tgt.Swing = 0.12;

            if (melee && !reflect && src != null)
            {
                double rate = (tgt.ThornsT > 0 ? tgt.ThornsV : 0) + tgt.Thorns;
                double back = dmg * rate;
                if (back > 1)
                    Hit(tgt, src, back, false, false, true);
            }

            return (int)dmg;
        }

        public static void Step(Body a, Body b, double dt, Random rng, double time)
        {
            if (dt <= 0 || a == null || b == null)
                return;
            Tick(a, dt);
            Tick(b, dt);
            Act(a, b, dt, rng, time);
            Act(b, a, dt, rng, time);
            a.X = Clamp(a.X);
            b.X = Clamp(b.X);
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
            body.Mana = Math.Min(body.ManaMax, body.Mana + body.ManaRegen * dt);
        }

        static void Act(Body self, Body other, double dt, Random rng, double time)
        {
            if (!self.Alive || !other.Alive || self.Stun > 0)
                return;

            bool stepped = TryClose(self, other);
            double dist = Math.Abs(self.X - other.X);
            bool close = dist < CastBand;
            if (!stepped && self.Pound && self.PoundCd <= 0 && close && self.Mana >= PoundCost)
            {
                self.Mana -= PoundCost;
                bool crit = rng.NextDouble() < self.Crit;
                Hit(self, other, self.Dmg * 1.15, true, crit, false, time);
                other.Stun = Math.Max(other.Stun, 0.5);
                double dir = Math.Sign(other.X - self.X);
                if (dir == 0)
                    dir = 1;
                other.X += dir * 2;
                self.PoundCd = 10 * self.CdMul;
                self.Callout = "Ground Pound";
                self.Swing = 0.28;
            }
            else if (!stepped && self.Ward && self.WardCd <= 0 && close && self.Mana >= WardCost)
            {
                self.Mana -= WardCost;
                if (!(self.Shield > 0))
                    self.Shield += Math.Round(self.Max * 0.4);
                self.ThornsV = Math.Max(self.ThornsV, 0.4);
                self.ThornsT = Math.Max(self.ThornsT, 6);
                self.WardCd = 16 * self.CdMul;
                self.Callout = "Ward Field";
                self.Swing = 0.2;
            }
            else if (!stepped && self.Wall && self.WallCd <= 0 && close)
                RaiseWall(self);

            dist = Math.Abs(self.X - other.X);
            if (dist <= self.Range + ReachPad)
            {
                if (self.AtkCd > 0)
                    return;
                bool crit = rng.NextDouble() < self.Crit;
                Hit(self, other, self.Dmg, true, crit, false, time);
                self.AtkCd = 1.0 / Math.Max(0.15, self.Rate);
                self.Callout = self.Name == "Vale" ? "Cleave" : "Heavy Sweep";
                self.Swing = 0.22;
                return;
            }

            double step = Math.Sign(other.X - self.X) * self.Speed * dt;
            self.X += step;
        }

        static double Clamp(double x)
        {
            if (x < 0.8)
                return 0.8;
            if (x > Lane - 0.8)
                return Lane - 0.8;
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
