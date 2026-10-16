using System;
using System.Collections.Generic;

namespace BattleTv.Menu
{
    public readonly struct Kit
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Tag;
        public readonly string Cls;
        public readonly string Color;
        public readonly string Bio;
        public readonly string Counter;
        public readonly string Atk;
        public readonly int Hp;
        public readonly float Dmg;
        public readonly float Rate;
        public readonly float Speed;
        public readonly float Armor;
        public readonly float Range;
        public readonly int Mana;
        public readonly string[] Moves;
        public readonly string[] Cards;
        public readonly bool Linked;

        public Kit(
            string id, string name, string tag, string cls, string color,
            string bio, string counter, string atk,
            int hp, float dmg, float rate, float speed, float armor, float range, int mana,
            string[] moves, string[] cards, bool linked)
        {
            Id = id;
            Name = name;
            Tag = tag;
            Cls = cls;
            Color = color;
            Bio = bio;
            Counter = counter;
            Atk = atk;
            Hp = hp;
            Dmg = dmg;
            Rate = rate;
            Speed = speed;
            Armor = armor;
            Range = range;
            Mana = mana;
            Moves = moves ?? Array.Empty<string>();
            Cards = cards ?? Array.Empty<string>();
            Linked = linked;
        }

        public string ClassLabel
        {
            get
            {
                if (Cls == "champ")
                    return "Champion";
                if (Cls == "pet")
                    return "Beast";
                return "Crypt creature";
            }
        }
    }

    /// <summary>
    /// The live 33-kit book. Linked is the plug. A kit enters the side lane only
    /// when Linked is true and the duel view has a body for that id.
    /// </summary>
    public static class LaneRoster
    {
        static readonly Dictionary<string, string> MoveNames = new Dictionary<string, string>(96);
        static readonly Dictionary<string, string> MoveText = new Dictionary<string, string>(96);
        static readonly Dictionary<string, string> CardNames = new Dictionary<string, string>(40);
        static readonly Dictionary<string, string> CardTags = new Dictionary<string, string>(40);
        static readonly Dictionary<string, string> CardText = new Dictionary<string, string>(40);

        public static readonly Kit[] All = Build();

        public static int LinkedCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < All.Length; i++)
                {
                    if (All[i].Linked)
                        n++;
                }
                return n;
            }
        }

        public static Kit ById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return default;
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                    return All[i];
            }
            return default;
        }

        public static Kit[] Of(string cls)
        {
            var n = 0;
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Cls == cls)
                    n++;
            }
            var list = new Kit[n];
            var w = 0;
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Cls == cls)
                    list[w++] = All[i];
            }
            return list;
        }

        public static string[] LinkedIds()
        {
            var list = new string[LinkedCount];
            var w = 0;
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Linked)
                    list[w++] = All[i].Id;
            }
            return list;
        }

        public static string[] Blind(IList<string> yours, int count, Random rng)
        {
            var pool = new List<string>();
            for (var i = 0; i < All.Length; i++)
            {
                if (!All[i].Linked)
                    continue;
                var taken = false;
                for (var y = 0; y < yours.Count; y++)
                {
                    if (yours[y] == All[i].Id)
                        taken = true;
                }
                if (!taken)
                    pool.Add(All[i].Id);
            }
            if (pool.Count < count)
                return null;
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
            var draw = new string[count];
            for (var i = 0; i < count; i++)
                draw[i] = pool[i];
            return draw;
        }

        public static string[] BlindBook(IList<string> yours, int count, Random rng)
        {
            var pool = new List<string>();
            for (var i = 0; i < All.Length; i++)
            {
                var taken = false;
                for (var y = 0; y < yours.Count; y++)
                {
                    if (yours[y] == All[i].Id)
                        taken = true;
                }
                if (!taken)
                    pool.Add(All[i].Id);
            }
            if (pool.Count < count || rng == null)
                return null;
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
            var draw = new string[count];
            for (var i = 0; i < count; i++)
                draw[i] = pool[i];
            return draw;
        }

        public static string MoveName(string id)
        {
            return MoveNames.TryGetValue(id, out var name) ? name : id;
        }

        public static string MoveLine(string id)
        {
            MoveText.TryGetValue(id, out var text);
            return text ?? "";
        }

        public static string CardName(string id)
        {
            return CardNames.TryGetValue(id, out var name) ? name : id;
        }

        public static string CardTag(string id)
        {
            return CardTags.TryGetValue(id, out var tag) ? tag : "";
        }

        public static string CardLine(string id)
        {
            CardText.TryGetValue(id, out var text);
            return text ?? "";
        }

        static Kit[] Build()
        {
            Move("rend", "Rend", "Tear the nearest foe and leave it bleeding.");
            Move("shieldwall", "Shield Wall", "Raise the door: shield yourself and pull every eye onto you.");
            Move("charge", "Squire's Charge", "Close the gap in one stride and strike on arrival.");
            Move("cover", "Cover", "Throw a shield over the ally in the worst shape.");
            Move("bulwark", "Bulwark", "Harden the whole line: nearby allies gain armor.");
            Move("shieldhook", "Shield Hook", "The shield's edge catches and turns you into her reach.");
            Move("unwrite", "Unwrite", "A blast that eats shields and hits them twice as hard.");
            Move("resonance", "Resonance Flask", "Break a flask of names: burst plus a weakness.");
            Move("chorusflask", "Chorus Flask", "One drink, four pulses: the whole line heals.");
            Move("pinvolley", "Pinning Volley", "Three arrows that root whatever they find.");
            Move("dash", "Dash", "A burst of Swift — the corridor shortens.");
            Move("needlerain", "Needle Rain", "A spread of needles that pass one body each.");
            Move("hymn", "Hymn", "The song the crypt cannot erase: stuns everything near.");
            Move("chorus", "Spiral Chorus", "Mend the line and hurry it along.");
            Move("weavebloom", "Weave Bloom", "Plant a bloom of regrowth under your team.");
            Move("geodesic", "Geodesic", "Lob the shortest true line over the stone, then blast.");
            Move("trueline", "True Line", "A spear of the shortest true line — it ignores armor.");
            Move("shockwave", "Shockwave", "Slam the floor: everything in reach is thrown back.");
            Move("wolffury", "Wolf Fury", "Fists first: rage and drink what you cut.");
            Move("doublespeak", "Doublespeak", "You are already gone — veil, and the next cut doubles.");
            Move("righttime", "Right-Time", "Warp the hour: foes slow, you hurry.");
            Move("borrow", "Borrowed Second", "Spend a second as a key: every cooldown resets.");
            Move("seal9", "Δ9 Seal", "The architect's seal: stuns the room, shields his own.");
            Move("fairplate", "Fair Plate", "Fairness as plate: reflect and thorn up.");
            Move("verdict", "Verdict", "Strike where the plate is already cracked.");
            Move("writpursuit", "Writ of Pursuit", "The writ runs faster than the guilty.");
            Move("undertow", "Undertow", "Drag them in and slow what has no footing.");
            Move("current", "Current", "The tide behind the line: allies hurry and stride.");
            Move("provenance", "Provenance", "Remember the name: a fallen ally rises again.");
            Move("standard", "Standard Raised", "Plants the standard: the line mends and moves quicker.");
            Move("wail", "Wail", "A thin cry that weakens what hears it and nails its feet down.");
            Move("draincloud", "Drain Cloud", "A cloud of unwriting — it hurts, it stays, and everything in it wades.");
            Move("pound", "Ground Pound", "Both fists down: the floor answers.");
            Move("wardfield", "Ward Field", "The crypt closes its ward around the gate.");
            Move("irongrip", "Iron Grip", "It reaches. The reach ends the argument.");
            Move("impfire", "Imp Fire", "A coal of ember that burns and leaves them limping.");
            Move("flicker", "Flicker", "Blink through the stone and arrive before they are ready.");
            Move("lobstone", "Lob Stone", "Hurled over the walls. It does not need a line.");
            Move("snatch", "Snatch", "Steal the cut — and the stride — and drink it.");
            Move("unmake", "Unmake", "Silence a name mid-sentence and hold it there while it burns.");
            Move("gravstep", "Grave Step", "It is behind you before the light changes.");
            Move("gateslam", "Gate Slam", "The gate answers. Nothing near it keeps its footing.");
            Move("gateward", "Gate Ward", "The door sets a ward. It is a pause, not a second life.");
            Move("callwards", "Call the Wards", "Two wardens step out of the dark.");
            Move("gate_haul", "Gate Haul", "The gate does not come to you. The gate decides.");
            Move("regalvolley", "Regal Volley", "A crown's three shells, lobbed wide.");
            Move("decree", "Decree", "The crown does not chase. It summons.");
            Move("anvil", "Anvil", "Bring the anvil down and let the coal do the rest.");
            Move("gatecannon", "Gate Cannon", "A shell of cold light from the gate itself.");
            Move("temperwork", "Temper Work", "The smith re-tempers what the fight has bent.");
            Move("heartbeat", "Heartbeat", "The heart of the crypt beats: it mends its own.");
            Move("cryptward", "Crypt Ward", "The beat puts a ward on everything that shares its blood.");
            Move("cryptsurge", "Crypt Surge", "The floor carries it forward.");
            Move("tidecrush", "Tide Crush", "The depth takes the room and drags it in.");
            Move("tithe", "Tithe", "The tithe-king is paid in what you were saving.");
            Move("collection", "Collection", "The tithe is taken where it stands.");
            Move("nameless", "Nameless", "Names go where it looks.");
            Move("lockseal", "The Lock Seals", "The whole arena is the door and it is closing.");
            Move("bite", "Pack Bite", "Dash in and hamstring — the pack is faster than you.");
            Move("roar", "Roar", "A roar that shakes the line and throws it back off its feet.");
            Move("soljump", "Solstride", "A long leap onto the one already hurt.");
            Move("maul", "Maul", "Ironhide comes down on everything in reach.");
            Move("maulgrip", "Maul Grip", "It takes your feet out and holds you where it wants you.");
            Move("stomp", "Stomp", "The ground answers the tusk.");
            Move("tuskcharge", "Tusk Charge", "The whole weight arrives at once.");
            Move("clamp", "Glassbarb Clamp", "A clamp of glass barb and a tail you did not see.");

            Card("might", "Tooth of the Door", "MIGHT", "+25% damage on every attack and move.");
            Card("iron", "Fair Plate", "IRON", "+3 armor.");
            Card("heart", "Wellspring", "WELL", "+25% maximum health.");
            Card("haste", "Right-Time Hands", "HASTE", "+22% attack speed and move cooldowns −15%.");
            Card("swift", "Path Stride", "STRIDE", "+18% move speed.");
            Card("leech", "Drink the Cut", "LEECH", "Heal for 15% of all damage dealt.");
            Card("thorns", "Bumper Thorn", "THORN", "Melee attackers take 30% of the damage back.");
            Card("regen", "Lattice Blood", "REGEN", "Regenerate 2.2 health a second.");
            Card("veil", "Veil Step", "VEIL", "18% of incoming attacks miss.");
            Card("fury", "Fury Hour", "FURY", "+45% damage once below 40% health.");
            Card("laststand", "Second Wind", "GIFT", "The first time you drop below 30%, heal 35% of maximum.");
            Card("warded", "Lattice Ward", "GIFT", "Enter the arena under a shield worth 30% of maximum health.");
            Card("secondlife", "Name Recall", "GIFT", "Rise once more at 40% health after being cut down.");
            Card("pierce", "Truth Lens", "PIERCE", "Attacks carry through one extra body.");
            Card("volley", "Open Volley", "VOLLEY", "+1 shot on every multi-shot move.");
            Card("echo", "Echo Round", "ECHO", "Every attack echoes for 40% after a beat.");
            Card("bulwark", "Door Flesh", "BULWARK", "Take 22% less damage while above 60% health.");
            Card("executioner", "Verdict Edge", "EXEC", "+70% damage against targets below 32% health.");
            Card("giant", "Deeper Well", "GIANT", "+40% maximum health, −12% move speed.");
            Card("nimble", "Ember Step", "NIMBLE", "−15% maximum health, +25% move speed.");
            Card("vampiric", "Cup of the Cut", "VAMP", "Recover 22% of maximum health on every kill.");
            Card("manaflow", "Chorus Charge", "RES", "+60% magic regeneration, +20% maximum magic.");
            Card("overclock", "Overclock", "CLOCK", "All move cooldowns −32%, −10% maximum health.");
            Card("aegisgift", "A New Accord", "AURA", "Allies gain +2 armor while you live.");
            Card("standard", "Hall Standard", "AURA", "Allies deal +12% damage while you live.");
            Card("focus", "Named Pulse", "FOCUS", "Your first move of the fight hits for double.");
            Card("lucky", "Tithe Sense", "LUCK", "+18% critical chance.");
            Card("brutal", "Rift Edge", "BRUTAL", "Critical hits strike for +80% instead of +50%.");
            Card("cleanse", "Unwrite Ink", "CLEANSE", "Shrug off the first stun, root or silence laid on you.");

            return new[]
            {
                K("kael", "Kael", "Gallant Blade", "champ", "#ef4444", "First tooth of the lock. Would not drop the door when the first circle went dark.", "Holds a line. Melted by artillery that never lets him close.", "Shard Volley", 805, 30f, 0.80f, 3.1f, 6f, 1.5f, 70, "rend,shieldwall,charge", "might,iron,vampiric"),
                K("vale", "Vale", "Aegis Knight", "champ", "#22d3ee", "Plate of the second circle. She learned to stand still so others could move.", "The wall of the roster. Kills nothing fast — punishes anything that tries.", "Cleave", 895, 26f, 0.72f, 3.3f, 8f, 1.6f, 60, "cover,bulwark,shieldwall,shieldhook", "bulwark,thorns,iron", true),
                K("orin", "Orin", "Black Sigil", "champ", "#fbbf24", "A chemist of names. Glass is cheaper than a forgotten friend.", "Eats shields. Dies to anything that reaches him.", "Sigil Shard", 600, 34f, 0.86f, 2.9f, 2f, 8.2f, 150, "unwrite,resonance,chorusflask", "might,focus,manaflow"),
                K("nia", "Nia", "Path Archer", "champ", "#4ade80", "She maps the corridors by running them. A slow warden is a closed door.", "Fastest feet in the arena. Rooted once and she is only an archer.", "Path Arrow", 707, 27f, 0.92f, 4.4f, 4f, 8.6f, 70, "pinvolley,dash,needlerain", "swift,lucky,volley"),
                K("lyra", "Lyra", "Spiral Bard", "champ", "#67e8f9", "Seat of spiral memory. The crypt cannot erase a song it has heard.", "Stuns a crowd and heals a crowd. No burst at all.", "Spiral Note", 645, 28f, 0.84f, 3.4f, 3f, 7.8f, 135, "hymn,chorus,weavebloom", "manaflow,regen,standard"),
                K("arkos", "Arkos", "Lattice Lancer", "champ", "#2dd4bf", "The spear is a geodesic: the shortest true line through a lie.", "Ignores walls and armor. Weak to being swarmed.", "Lance Thrust", 918, 35.34f, 0.78f, 3.2f, 10f, 1.9f, 80, "geodesic,trueline,dash", "pierce,might,nimble"),
                K("d9ra", "D9ra", "Wolf Monk", "champ", "#f87171", "Wolf-edge of the council. Fists first. The crypt respects what will not flinch.", "Highest damage per second on the roster. No answer at range.", "Wolf Fist", 830, 38f, 0.88f, 3.8f, 5f, 1.4f, 50, "shockwave,wolffury,charge", "fury,brutal,leech"),
                K("srath", "Srath", "Shadow Needle", "champ", "#86efac", "Sentinel of doublespeak. She steals the vial back before the thief knows it left.", "Hard to hit, hits through a line. The softest body in the roster.", "Needle", 550, 29f, 0.90f, 4.2f, 2f, 7.6f, 80, "doublespeak,needlerain,dash", "veil,lucky,echo"),
                K("kairos", "Kairos", "Hour Mage", "champ", "#c4b5fd", "Keeper of right-time. A vial in his hand is also a door.", "Slows the whole arena and never runs out of cooldowns. Thin as paper.", "Hour Bolt", 600, 33f, 0.82f, 3.3f, 2f, 8.4f, 155, "righttime,borrow,seal9", "haste,manaflow,overclock"),
                K("justicae", "Justicae", "Accord Knight", "champ", "#e2e8f0", "Fairness as plate. She will not open a seal that would crush the unnamed.", "Reflects everything thrown at her and finishes what is already hurt.", "Accord Blade", 1049, 27f, 0.74f, 3.0f, 11f, 1.7f, 90, "fairplate,verdict,bulwark,writpursuit", "executioner,bulwark,thorns"),
                K("seidon", "Seidon", "Tide Seer", "champ", "#22d3ee", "Depth and current. Gates listen to him because the lattice learned to drink.", "Drags a team out of position. Wants a crowd, does badly alone.", "Tide Bolt", 725, 30f, 0.80f, 3.5f, 5f, 8.0f, 120, "undertow,current,geodesic", "standard,manaflow,warded"),
                K("sancora", "Sancora", "Weave Chemist", "champ", "#fde68a", "Collective healing nexus. Her flask is a chorus: one drink, four pulses.", "Keeps four bodies standing. Brings no damage of her own.", "Chemist Vial", 580, 28f, 0.78f, 3.2f, 4f, 8.0f, 145, "chorusflask,weavebloom,cover", "regen,giant,aegisgift"),
                K("lightfather", "Lightfather", "Architect · Δ9", "champ", "#fbbf24", "He does not replace the four teeth — he remembers why the door was cut.", "Raises the dead and shuts a room down. Slow to come online.", "Seal Bolt", 660, 27f, 0.84f, 3.4f, 4f, 8.0f, 160, "seal9,provenance,cover,standard", "standard,secondlife,warded"),

                K("wraith", "Wraith", "Crypt Fodder", "beast", "#c4b5fd", "The crypt's first draft of a soldier. It throws what it has no hand for.", "Cheap ranged bodies that nail your feet down. Two are a headache, five are a wall.", "Wraith Bolt", 650, 31f, 0.86f, 2.6f, 4f, 8.0f, 60, "wail,draincloud", "haste,nimble,leech"),
                K("brute", "Brute", "Crypt Fodder", "beast", "#94a3b8", "Slow, wide and certain. It walks the shortest path to whatever it was told to want.", "Soaks a lane. Outrun and outranged by nearly everyone.", "Heavy Sweep", 805, 34f, 0.66f, 2.0f, 6f, 1.5f, 40, "pound,wardfield,irongrip", "iron,giant,thorns", true),
                K("imp", "Imp", "Crypt Fodder", "beast", "#fb923c", "Fast, small, and holding a coal it intends to give you.", "Fast, burns, and leaves you limping after it. Still dies to one good hit.", "Ember Spit", 724, 34.8f, 1.01f, 2.9f, 6f, 7.4f, 60, "impfire,flicker", "swift,lucky,nimble"),
                K("hurler", "Hurler", "Crypt Artillery", "beast", "#a3a3a3", "It never wants a line of sight. The stone goes over the wall or not at all.", "Hits the back line from behind stone. Helpless up close.", "Stone Toss", 815, 33f, 0.72f, 2.4f, 5f, 5.6f, 70, "lobstone,pound", "might,bulwark,giant"),
                K("shade", "Shade", "Veilkin", "beast", "#818cf8", "It is in two places and only one of them is real.", "Blinks onto you before you are ready and out again. Fragile when caught in a corner.", "Shade Claw", 781, 35f, 0.92f, 3.2f, 5f, 1.6f, 60, "flicker,snatch", "veil,leech,nimble"),
                K("thief", "Thief", "Tithe-Thief", "beast", "#facc15", "It takes the vial, the coin, and the courage, and is gone before the roll.", "The fastest creature alive — it takes your stride with your coin. Never wins a stand-up fight.", "Quick Stab", 790, 36f, 1.05f, 3.6f, 6f, 1.6f, 50, "snatch,flicker", "swift,vampiric,lucky"),
                K("drain", "Drain", "The Unwriting", "beast", "#7c3aed", "It does not kill you. It takes the sentence you were in.", "Heals off everything it touches and holds it in place while it does. Bring armor and burst.", "Drain Touch", 875, 37f, 0.78f, 2.9f, 7f, 2.0f, 90, "unmake,draincloud,gravstep", "leech,regen,might"),
                K("gate", "Gate", "Nexus Guardian", "beast", "#38bdf8", "Half door, half animal, entirely unwilling.", "The anvil of the roster. Everything it does happens slowly.", "Gate Slam", 1020, 40f, 0.60f, 1.6f, 7f, 1.9f, 80, "gateslam,gateward,callwards,gate_haul", "iron,thorns,giant"),
                K("crown", "Crown", "Nexus Guardian", "beast", "#fbbf24", "It wears the floor it was built on and lobs the rest at you.", "Bombards a whole team from behind cover. Needs its own cover gone.", "Regal Fist", 1125, 36f, 0.62f, 1.9f, 6f, 1.8f, 110, "regalvolley,lobstone,decree", "might,bulwark,heart"),
                K("smith", "Smith", "Nexus Guardian", "beast", "#f97316", "It forges in the dark. What it makes is mostly for you.", "Highest single-hit damage on the roster. Slow as a workbench.", "Forge Bolt", 1175, 42f, 0.64f, 1.7f, 7f, 7.8f, 100, "anvil,gatecannon,temperwork", "might,brutal,iron"),
                K("heartboss", "Heart of the Crypt", "Nexus Guardian", "beast", "#ef4444", "Ten thousand feet of the crypt, and it beats.", "Heals its own team while it fights. Kill it first or not at all.", "Heart Strike", 1320, 38f, 0.68f, 2.0f, 7f, 1.8f, 90, "heartbeat,cryptward,cryptsurge", "giant,regen,vampiric"),
                K("levi", "Leviathan", "Nexus Guardian", "beast", "#22d3ee", "The depth stood up and walked into the arena because it was invited.", "Pulls a team apart one body at a time. Fragile for a guardian.", "Deep Bolt", 1240, 40f, 0.72f, 2.1f, 6f, 7.6f, 120, "tidecrush,undertow", "might,swift,leech"),
                K("tithe", "Tithe-King", "Nexus Guardian", "beast", "#facc15", "Every hall pays it. It collects in the middle of the fight.", "Turns your own health into its health. Burst it or bleed.", "Toll Claw", 1150, 39f, 0.98f, 2.3f, 5f, 2.0f, 90, "tithe,snatch,collection", "vampiric,executioner,swift"),
                K("unnamer", "Name-Eater", "Nexus Guardian", "beast", "#a855f7", "It has no interest in your body. It is here for the name on it.", "Silences healers and casters. No answer to plain damage.", "Unname", 1290, 37f, 0.76f, 2.4f, 6f, 8.2f, 130, "nameless,unmake", "might,overclock,leech"),
                K("lock", "The Lock", "Super Boss", "beast", "#e2e8f0", "The door was never finished. The last thing the Accord built is this.", "The strongest single body in the game. One body still loses to five.", "Lock Bolt", 1725, 46f, 0.66f, 1.8f, 11f, 8.4f, 120, "lockseal,gatecannon", "iron,bulwark,giant"),

                K("wolf", "Ashmane", "Wolf", "pet", "#94a3b8", "Dash · bite. It arrives before the decision does.", "Jumps the weakest body and hamstrings it for the pack. Still too eager to survive long.", "Bite", 860, 38f, 0.96f, 3.9f, 7f, 1.5f, 40, "bite,roar", "swift,fury,leech"),
                K("lion", "Solstride", "Lion", "pet", "#fbbf24", "Jump · roar · claw. It picks its fight out loud.", "Hunts the wounded all match. Ignores the healthy one.", "Claw", 855, 34f, 0.90f, 3.5f, 10f, 1.6f, 50, "soljump,roar", "lucky,brutal,might"),
                K("bear", "Ironhide", "Bear", "pet", "#fb923c", "Swipe · maul. The pack's door.", "A second wall for the price of a beast. No reach and no tricks.", "Swipe", 790, 36.96f, 0.74f, 2.3f, 9f, 1.7f, 40, "maul,wardfield,maulgrip", "iron,giant,thorns"),
                K("elephant", "Tuskward", "Elephant", "pet", "#67e8f9", "Stomp. It does not need a second move.", "The heaviest thing on legs. Everything outruns it.", "Tusk", 886, 35f, 0.68f, 2.5f, 8f, 1.9f, 40, "stomp,pound,tuskcharge", "iron,bulwark,heart"),
                K("scorpion", "Glassbarb", "Scorpion", "pet", "#c4b5fd", "Clamp · tail. The barb is the part that matters.", "Poisons everything it touches. Will lose a long fight.", "Tail Barb", 560, 33f, 1.02f, 2.9f, 6f, 1.6f, 60, "clamp,flicker", "thorns,fury,veil")
            };
        }

        static void Move(string id, string name, string text)
        {
            MoveNames[id] = name;
            MoveText[id] = text;
        }

        static void Card(string id, string name, string tag, string text)
        {
            CardNames[id] = name;
            CardTags[id] = tag;
            CardText[id] = text;
        }

        static Kit K(
            string id, string name, string tag, string cls, string color,
            string bio, string counter, string atk,
            int hp, float dmg, float rate, float speed, float armor, float range, int mana,
            string moves, string cards, bool linked = false)
        {
            return new Kit(id, name, tag, cls, color, bio, counter, atk, hp, dmg, rate, speed, armor, range, mana, Split(moves), Split(cards), linked);
        }

        static string[] Split(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return Array.Empty<string>();
            return csv.Split(',');
        }
    }
}
