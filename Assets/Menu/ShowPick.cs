using System;
using System.Collections.Generic;

namespace BattleTv.Menu
{
    public static class ShowPick
    {
        public static readonly int[] Modes = { 1, 2, 3, 5 };
        public static int Mode = 1;
        public static string Filter = "champ";
        public static string Inspect = "vale";
        public static readonly List<string> Yours = new List<string>(5);
        public static readonly List<string> Foes = new List<string>(5);
        public static bool Autonomous;
        public static int Reign;
        public static string ReignId = "";
        static int _showCursor;
        static string _pendingWinner = "";

        public static void BeginDraft(int mode)
        {
            Autonomous = false;
            Mode = Legal(mode);
            if (string.IsNullOrEmpty(Filter))
                Filter = "champ";
            while (Yours.Count > Mode)
                Yours.RemoveAt(Yours.Count - 1);
            Foes.Clear();
        }

        public static void SetMode(int mode)
        {
            BeginDraft(mode);
        }

        public static bool Toggle(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            if (Yours.Contains(id))
            {
                Yours.Remove(id);
                Foes.Clear();
                return true;
            }
            var kit = LaneRoster.ById(id);
            if (kit.Id == null)
                return false;
            if (Mode <= 1 && !LaneRoster.InLane(id))
                return false;
            if (Yours.Count >= Mode)
                return false;
            Yours.Add(id);
            Foes.Clear();
            return true;
        }

        public static void Drop(string id)
        {
            Yours.Remove(id);
            Foes.Clear();
        }

        public static void Clear()
        {
            Yours.Clear();
            Foes.Clear();
        }

        public static void RandomDraw(Random rng)
        {
            if (rng == null)
                rng = new Random();
            Yours.Clear();
            Foes.Clear();
            var lane = new List<string>();
            var all = LaneRoster.All;
            for (var i = 0; i < all.Length; i++)
            {
                if (LaneRoster.InLane(all[i].Id))
                    lane.Add(all[i].Id);
            }
            Shuffle(lane, rng);
            for (var i = 0; i < lane.Count && Yours.Count < Mode; i++)
                Yours.Add(lane[i]);
            if (Yours.Count > 0)
                Inspect = Yours[0];
        }

        public static string BlockReason()
        {
            var label = Label(Mode);
            if (Yours.Count < Mode)
                return "Pick " + (Mode - Yours.Count) + " more to field " + label + ".";
            if (Yours.Count > Mode)
                return "Drop " + (Yours.Count - Mode) + " to field " + label + ".";
            for (var i = 0; i < Yours.Count; i++)
            {
                var kit = LaneRoster.ById(Yours[i]);
                if (kit.Id == null)
                    return (Yours[i] ?? "That kit") + " is not in the book.";
                if (Mode <= 1 && !LaneRoster.InLane(Yours[i]))
                    return (kit.Name ?? Yours[i]) + " is not in the lane yet.";
            }
            if (Mode > 1)
                return null;
            var linked = LaneRoster.LinkedCount;
            var need = Mode * 2;
            if (linked < need)
                return "The lane is still one body each. " + label + " needs " + need + " linked kits. " + linked + " are in the lane.";
            if (linked - Yours.Count < Mode)
                return "Not enough other linked kits for a blind side.";
            return null;
        }

        public static bool TryField(Random rng, out string block)
        {
            Autonomous = false;
            block = BlockReason();
            Foes.Clear();
            if (block != null)
                return false;
            if (rng == null)
                rng = new Random();
            var classic = Mode <= 1 && Yours.Count == 1 && (Yours[0] == "vale" || Yours[0] == "brute");
            var foe = Mode > 1
                ? LaneRoster.BlindBook(Yours, Mode, rng)
                : classic
                    ? LaneRoster.Blind(Yours, Mode, rng)
                    : LaneRoster.BlindLane(Yours, Mode, rng);
            if (foe == null)
            {
                block = "Not enough other kits for a blind side.";
                return false;
            }
            for (var i = 0; i < foe.Length; i++)
                Foes.Add(foe[i]);
            if (Mode <= 1 && !LanePair())
            {
                Foes.Clear();
                block = "That body is not painted for the side lane yet.";
                return false;
            }
            return true;
        }

        static bool LanePair()
        {
            if (Mode != 1 || Yours.Count != 1 || Foes.Count != 1)
                return false;
            return LaneRoster.InLane(Yours[0]) && LaneRoster.InLane(Foes[0]);
        }

        public static bool PaintsValeAndBrute()
        {
            if (Mode != 1 || Yours.Count != 1 || Foes.Count != 1)
                return false;
            var a = Yours[0];
            var b = Foes[0];
            return (a == "vale" && b == "brute") || (a == "brute" && b == "vale");
        }

        public static bool BeginAutonomous(Random rng)
        {
            if (!LaneRoster.ById("vale").Linked || !LaneRoster.ById("brute").Linked)
                return false;
            Autonomous = true;
            Mode = 1;
            Reign = 0;
            ReignId = "";
            _pendingWinner = "";
            _showCursor = 1;
            var roster = LaneRoster.LaneIds();
            for (var i = 0; i < roster.Length; i++)
            {
                if (roster[i] == "brute")
                    _showCursor = i;
            }
            Yours.Clear();
            Foes.Clear();
            Yours.Add("vale");
            Foes.Add("brute");
            return true;
        }

        /// <summary>
        /// The winner stays. The next painted body in show order steps in.
        /// A draw advances the pair. The opening card is still Vale versus Brute.
        /// </summary>
        public static bool AdvanceCard()
        {
            if (!Autonomous)
                return false;
            var roster = LaneRoster.LaneIds();
            if (roster.Length < 2)
                return false;
            var stay = "";
            if (!string.IsNullOrEmpty(_pendingWinner) && LaneRoster.InLane(_pendingWinner))
                stay = _pendingWinner;
            _pendingWinner = "";
            string foe;
            if (stay.Length > 0)
            {
                foe = "";
                for (var n = 0; n < roster.Length; n++)
                {
                    _showCursor = (_showCursor + 1) % roster.Length;
                    if (roster[_showCursor] != stay)
                    {
                        foe = roster[_showCursor];
                        break;
                    }
                }
                if (foe.Length == 0)
                    return false;
                Yours.Clear();
                Foes.Clear();
                Yours.Add(stay);
                Foes.Add(foe);
            }
            else
            {
                _showCursor = (_showCursor + 1) % roster.Length;
                var lead = roster[_showCursor];
                foe = roster[(_showCursor + 1) % roster.Length];
                Yours.Clear();
                Foes.Clear();
                Yours.Add(lead);
                Foes.Add(foe);
            }
            Mode = 1;
            return true;
        }

        public static void StopShow()
        {
            Autonomous = false;
            Reign = 0;
            ReignId = "";
            _pendingWinner = "";
            _showCursor = 0;
            Yours.Clear();
            Foes.Clear();
        }

        // The mode page keeps a drafted lineup. An autonomous card was forced, so leaving it clears that card.
        public static void LeaveToMode()
        {
            if (Autonomous)
                StopShow();
            else
                Foes.Clear();
        }

        public static void Focus(string id)
        {
            var kit = LaneRoster.ById(id);
            if (kit.Id == null)
                return;
            Inspect = kit.Id;
            Filter = kit.Cls;
        }

        public static void RememberResult(string winnerId)
        {
            if (!Autonomous)
                return;
            if (string.IsNullOrEmpty(winnerId))
            {
                _pendingWinner = "";
                return;
            }
            _pendingWinner = winnerId;
            if (winnerId == ReignId)
                Reign++;
            else
            {
                ReignId = winnerId;
                Reign = 1;
            }
        }

        public static string ReignLine()
        {
            if (!Autonomous)
                return "";
            if (string.IsNullOrEmpty(ReignId) || Reign <= 0)
                return "Battle TV · next card follows.";
            var name = ReignId == "brute" ? "Brute" : LaneRoster.ById(ReignId).Name ?? "Vale";
            return name + " reign " + Reign + " · next card follows.";
        }

        public static string Label(int mode)
        {
            if (mode == 2)
                return "2v2";
            if (mode == 3)
                return "3v3";
            if (mode == 5)
                return "5v5";
            return "1v1";
        }

        static int Legal(int mode)
        {
            for (var i = 0; i < Modes.Length; i++)
            {
                if (Modes[i] == mode)
                    return mode;
            }
            return 1;
        }

        static void Shuffle(List<string> pool, Random rng)
        {
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
        }
    }
}
