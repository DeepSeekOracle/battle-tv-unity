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
        public double HitRadius = 16;
        public int Face = 1;
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
        public bool Gun;
        public bool Token;
        public double Ttl;
        public double Crit = 0.1;
        public double CritMul = 1.5;
        public double CdMul = 1;
        public double Splash;
        public double Bulwark;
        public double ThornsMod;
        public double Leech;
        public double Vamp;
        public double Fury;
        public double Veil;
        public double VeilT;
        public double VeilV;
        public double Execute;
        public double PassiveRegen;
        public double StartShield;
        public double GivesArmor;
        public double GivesDmg;
        public int Pierce;
        public int Volley;
        public double Echo;
        public bool FirstDouble;
        public bool FirstUsed;
        public bool CleanseCard;
        public bool UsedCleanse;
        public bool SecondLife;
        public bool UsedRevive;
        public bool LastStand;
        public bool UsedLastStand;
        public double Stun;
        public double Root;
        public double Silence;
        public double SlowT;
        public double SlowV;
        public double SwiftT;
        public double SwiftV;
        public double HasteT;
        public double HasteV;
        public double RageT;
        public double RageV;
        public double WeakT;
        public double WeakV;
        public double TauntT;
        public double ThornsT;
        public double ThornsV;
        public double WoundT;
        public double WoundV;
        public double ArmorBuff;
        public double ArmorBuffT;
        public double BurnT;
        public double BurnDps;
        public double BleedT;
        public double BleedDps;
        public double RegenT;
        public double RegenHps;
        public double LifestealT;
        public double LifestealV;
        public int CcHardN;
        public double CcHardAt;
        public int CcSoftN;
        public double CcSoftAt;
        public System.Collections.Generic.List<ArenaLogic.MoveDef> Moves;
        public double[] Cds = System.Array.Empty<double>();
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
        public ArenaBody Src;
        public double Angle;
        public double Speed;
        public double Life = 2;
        public string Name = "";
        public bool Lob;
        public double Tx;
        public double Ty;
        public double Radius;
        public bool Opening;
        public bool IgnoreArmor;
        public ArenaLogic.Status St;
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
        public static readonly List<ArenaBody> Bodies = new List<ArenaBody>(10);
        public static readonly List<ArenaBolt> Bolts = new List<ArenaBolt>(16);
        public static double Time;
        public static bool Over;
        public static bool Lane;
        public static string Title = "";
        public static string Detail = "";

        public static void Begin(IList<string> yours, IList<string> foes, int seed, bool lane = false)
        {
            Bodies.Clear();
            Bolts.Clear();
            Time = 0;
            Over = false;
            Lane = lane;
            Title = "";
            Detail = "";
            ArenaLogic.Reset(seed);
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
            ArenaLogic.Advance(dt);
            for (var n = 0; n < 8; n++)
                Ease();
            if (Lane)
            {
                for (var i = 0; i < Bodies.Count; i++)
                    Hold(Bodies[i]);
            }
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
                    Y = Lane ? WorldH * 0.5 : (3 + slot * 4) * Tile
                };
                ArenaLogic.Dress(body, kit);
                Bodies.Add(body);
            }
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
                    var dy = Lane ? 0 : b.Y - a.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    var sep = Math.Max(Sep, a.HitRadius + b.HitRadius + 8);
                    if (dist >= sep || dist < 0.001)
                        continue;
                    var push = (sep - dist) * 0.55;
                    a.X -= dx / dist * push;
                    a.Y -= dy / dist * push;
                    b.X += dx / dist * push;
                    b.Y += dy / dist * push;
                    Hold(a);
                    Hold(b);
                }
            }
        }

        public static void Hold(ArenaBody body)
        {
            if (body.X < Inset)
                body.X = Inset;
            if (body.Y < Inset)
                body.Y = Inset;
            if (body.X > WorldW - Inset)
                body.X = WorldW - Inset;
            if (body.Y > WorldH - Inset)
                body.Y = WorldH - Inset;
            if (Lane)
                body.Y = WorldH * 0.5;
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
