using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Menu
{
    public sealed class TeamSelectController : MonoBehaviour
    {
        static readonly Color Gold = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
        static readonly Color Ink = new Color(0x1a / 255f, 0x12 / 255f, 0x06 / 255f, 1f);
        static readonly Color Paper = new Color(0xf6 / 255f, 0xe7 / 255f, 0xc1 / 255f, 1f);
        static readonly Color Panel = new Color(0.04f, 0.03f, 0.02f, 0.82f);

        Font _font;
        RectTransform _list;
        RectTransform _dossier;
        RectTransform _tray;
        Text _title;
        Text _need;
        Text _status;
        Button _fight;
        Text _fightLabel;
        readonly List<Button> _modes = new List<Button>(4);
        float _listScroll;

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WarmFont(_font);
            if (string.IsNullOrEmpty(ShowPick.Filter))
                ShowPick.Filter = "champ";
            if (LaneRoster.ById(ShowPick.Inspect).Id == null)
                ShowPick.Inspect = "vale";
            BuildShell();
            Refresh();
        }

        void BuildShell()
        {
            var shell = Child(transform, "TeamShell");
            var shellRect = shell.GetComponent<RectTransform>();
            shellRect.anchorMin = Vector2.zero;
            shellRect.anchorMax = Vector2.one;
            shellRect.offsetMin = new Vector2(16f, 58f);
            shellRect.offsetMax = new Vector2(-16f, -46f);

            var head = PanelBox(shell.transform, "Head", new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            var back = Button(head, "BACK", new Vector2(0.012f, 0.18f), new Vector2(0.12f, 0.82f), false);
            back.onClick.AddListener(() => SceneManager.LoadScene("GameMenu"));
            _title = Words(head, "Title", new Vector2(0.14f, 0.46f), new Vector2(0.46f, 0.92f), 28, TextAnchor.MiddleLeft);
            _need = Words(head, "Need", new Vector2(0.14f, 0.06f), new Vector2(0.98f, 0.46f), 16, TextAnchor.MiddleLeft);
            _need.color = Gold;
            _need.verticalOverflow = VerticalWrapMode.Truncate;
            for (var i = 0; i < ShowPick.Modes.Length; i++)
            {
                var mode = ShowPick.Modes[i];
                var x0 = 0.50f + i * 0.122f;
                var button = Button(head, ShowPick.Label(mode), new Vector2(x0, 0.52f), new Vector2(x0 + 0.11f, 0.90f), false);
                button.onClick.AddListener(() =>
                {
                    ShowPick.SetMode(mode);
                    Refresh();
                });
                _modes.Add(button);
            }

            var filters = PanelBox(shell.transform, "Filters", new Vector2(0f, 0.78f), new Vector2(0.40f, 0.855f));
            AddFilter(filters, "Champions", "champ", 0.01f, 0.30f);
            AddFilter(filters, "Creatures", "beast", 0.32f, 0.62f);
            AddFilter(filters, "Beasts", "pet", 0.64f, 0.84f);
            var random = Button(filters, "Random", new Vector2(0.85f, 0.12f), new Vector2(0.995f, 0.88f), false);
            random.onClick.AddListener(() =>
            {
                ShowPick.RandomDraw(new System.Random());
                if (ShowPick.Yours.Count > 0)
                    ShowPick.Focus(ShowPick.Yours[0]);
                Refresh();
            });

            var clearHost = PanelBox(shell.transform, "ClearHost", new Vector2(0.405f, 0.78f), new Vector2(0.52f, 0.855f));
            var clear = Button(clearHost, "Clear", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f), false);
            clear.onClick.AddListener(() =>
            {
                ShowPick.Clear();
                Refresh();
            });

            _list = Scroll(shell.transform, "Roster", new Vector2(0f, 0.16f), new Vector2(0.40f, 0.77f), true);
            _dossier = Scroll(shell.transform, "Dossier", new Vector2(0.41f, 0.16f), new Vector2(1f, 0.855f), false);

            var foot = PanelBox(shell.transform, "Foot", new Vector2(0f, 0f), new Vector2(1f, 0.15f));
            _tray = Child(foot, "Tray").GetComponent<RectTransform>();
            _tray.anchorMin = new Vector2(0.012f, 0.12f);
            _tray.anchorMax = new Vector2(0.62f, 0.88f);
            _tray.offsetMin = Vector2.zero;
            _tray.offsetMax = Vector2.zero;
            var trayLayout = _tray.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.spacing = 8f;
            trayLayout.childAlignment = TextAnchor.MiddleLeft;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = true;

            _status = Words(foot, "Status", new Vector2(0.63f, 0.52f), new Vector2(0.995f, 0.92f), 15, TextAnchor.MiddleRight);
            _status.horizontalOverflow = HorizontalWrapMode.Overflow;
            _status.verticalOverflow = VerticalWrapMode.Truncate;
            _status.resizeTextForBestFit = false;
            _status.gameObject.AddComponent<RectMask2D>();
            _fight = Button(foot, "FIGHT", new Vector2(0.74f, 0.08f), new Vector2(0.99f, 0.48f), true);
            _fightLabel = _fight.GetComponentInChildren<Text>(true);
            _fight.onClick.AddListener(Fight);
        }

        void AddFilter(Transform parent, string label, string cls, float x0, float x1)
        {
            var button = Button(parent, label, new Vector2(x0, 0.12f), new Vector2(x1, 0.88f), false);
            button.onClick.AddListener(() =>
            {
                ShowPick.Filter = cls;
                var looking = LaneRoster.ById(ShowPick.Inspect);
                var list = LaneRoster.Of(cls);
                if ((looking.Id == null || looking.Cls != cls) && list.Length > 0)
                    ShowPick.Inspect = list[0].Id;
                Refresh();
            });
        }

        void Fight()
        {
            if (!ShowPick.TryField(new System.Random(), out _))
            {
                Refresh();
                return;
            }
            SceneManager.LoadScene(ShowPick.Mode <= 1 ? "Duel" : "Arena");
        }

        void Refresh()
        {
            Canvas.ForceUpdateCanvases();
            var list = LaneRoster.Of(ShowPick.Filter);
            var looking = LaneRoster.ById(ShowPick.Inspect);
            if (looking.Id == null)
            {
                looking = list.Length > 0 ? list[0] : LaneRoster.All[0];
                ShowPick.Inspect = looking.Id;
            }

            _title.text = "Team select  " + ShowPick.Yours.Count + "/" + ShowPick.Mode;
            var block = ShowPick.BlockReason();
            _need.text = block ?? (ShowPick.Mode <= 1
                ? "Blind draw. Both sides stay on their own."
                : "Blind draw onto the sand. A mark stands in until that body is painted.");
            _status.text = block == null
                ? Clip(_status, ShowPick.Label(ShowPick.Mode) + "  ·  " + Names(ShowPick.Yours))
                : "Lane not ready";
            _fight.interactable = block == null;
            if (_fightLabel != null)
                _fightLabel.text = block == null ? "FIGHT" : "NOT READY";

            for (var i = 0; i < _modes.Count; i++)
                PaintMode(_modes[i], ShowPick.Modes[i] == ShowPick.Mode);
            PaintFilter("Champions", ShowPick.Filter == "champ");
            PaintFilter("Creatures", ShowPick.Filter == "beast");
            PaintFilter("Beasts", ShowPick.Filter == "pet");

            if (_list != null)
                _listScroll = _list.anchoredPosition.y;
            Wipe(_list);
            Wipe(_dossier);
            Wipe(_tray);

            for (var i = 0; i < list.Length; i++)
                RosterRow(list[i]);
            var listLayout = _list.GetComponent<VerticalLayoutGroup>();
            var height = list.Length * 64f + 8f;
            _list.sizeDelta = new Vector2(0f, height);
            if (listLayout != null)
                listLayout.padding = new RectOffset(6, 6, 6, 6);
            _list.anchoredPosition = new Vector2(0f, _listScroll);

            Dossier(looking);
            if (ShowPick.Yours.Count == 0)
            {
                var empty = Words(_tray, "Empty", Vector2.zero, Vector2.one, 16, TextAnchor.MiddleLeft);
                empty.text = "No one fielded yet.";
                empty.color = new Color(Paper.r, Paper.g, Paper.b, 0.7f);
            }
            else
            {
                for (var i = 0; i < ShowPick.Yours.Count; i++)
                    Slot(ShowPick.Yours[i]);
            }
        }

        void RosterRow(Kit kit)
        {
            var picked = ShowPick.Yours.Contains(kit.Id);
            var on = ShowPick.Inspect == kit.Id;
            var soon = !kit.Linked && ShowPick.Mode <= 1;
            var row = Child(_list, kit.Id);
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 58f);
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.minHeight = 58f;
            var image = row.AddComponent<Image>();
            image.color = on ? new Color(0.18f, 0.12f, 0.05f, 0.95f) : new Color(0.08f, 0.06f, 0.04f, 0.9f);
            var button = row.AddComponent<Button>();
            button.targetGraphic = image;
            var id = kit.Id;
            button.onClick.AddListener(() =>
            {
                ShowPick.Inspect = id;
                Refresh();
            });
            var swatch = Child(row.transform, "Swatch");
            var swatchRect = swatch.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0.02f, 0.12f);
            swatchRect.anchorMax = new Vector2(0.16f, 0.88f);
            swatchRect.offsetMin = Vector2.zero;
            swatchRect.offsetMax = Vector2.zero;
            var face = swatch.AddComponent<Image>();
            var still = StillFor(kit.Id);
            if (!FighterBodies.Settled(kit.Id))
                StartCoroutine(WarmFace(kit.Id));
            face.sprite = still;
            face.color = still != null ? Color.white : Hex(kit.Color);
            face.preserveAspect = still != null;
            face.raycastTarget = false;
            var label = Words(row.transform, "Name", new Vector2(0.18f, 0.46f), new Vector2(0.98f, 0.94f), 18, TextAnchor.MiddleLeft);
            label.text = kit.Name;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = soon ? new Color(Paper.r, Paper.g, Paper.b, 0.55f) : Paper;
            var tag = Words(row.transform, "Tag", new Vector2(0.18f, 0.06f), new Vector2(0.98f, 0.48f), 13, TextAnchor.MiddleLeft);
            tag.text = kit.Tag + (soon ? "   ·   soon" : "");
            tag.supportRichText = false;
            tag.horizontalOverflow = HorizontalWrapMode.Overflow;
            tag.verticalOverflow = VerticalWrapMode.Truncate;
            tag.color = soon ? new Color(Gold.r, Gold.g, Gold.b, 0.45f) : Gold;
            if (picked)
                image.color = new Color(Gold.r, Gold.g, Gold.b, 0.28f);
        }

        void Dossier(Kit kit)
        {
            var who = Words(_dossier, "Who", new Vector2(0f, 1f), new Vector2(1f, 1f), 22, TextAnchor.MiddleLeft);
            who.rectTransform.pivot = new Vector2(0.5f, 1f);
            who.rectTransform.anchoredPosition = new Vector2(0f, -54f);
            who.rectTransform.sizeDelta = new Vector2(-32f, 30f);
            who.text = kit.Name;
            who.supportRichText = false;
            who.horizontalOverflow = HorizontalWrapMode.Overflow;
            who.verticalOverflow = VerticalWrapMode.Truncate;
            who.gameObject.AddComponent<RectMask2D>();
            var body = Words(_dossier, "Body", new Vector2(0f, 1f), new Vector2(1f, 1f), 18, TextAnchor.UpperLeft);
            body.supportRichText = false;
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            var lines = new List<string>(24);
            lines.Add(kit.ClassLabel.ToUpperInvariant());
            lines.Add(kit.Tag);
            lines.Add("");
            lines.Add(kit.Bio);
            lines.Add("");
            lines.Add(kit.Counter);
            lines.Add("");
            lines.Add(kit.Linked ? "In the lane." : "Not in the lane yet.");
            lines.Add("Basic attack: " + kit.Atk);
            lines.Add("");
            lines.Add("Health " + kit.Hp + "    Damage " + kit.Dmg.ToString("0.#") + "    Attacks " + kit.Rate.ToString("0.00") + "/s");
            lines.Add("Speed " + kit.Speed.ToString("0.0") + "    Armor " + kit.Armor.ToString("0.#") + "    Range " + kit.Range.ToString("0.0") + "    Mana " + kit.Mana);
            lines.Add("");
            lines.Add("MOVES");
            for (var i = 0; i < kit.Moves.Length; i++)
                lines.Add(LaneRoster.MoveName(kit.Moves[i]) + " — " + LaneRoster.MoveLine(kit.Moves[i]));
            lines.Add("");
            lines.Add("CARDS");
            for (var i = 0; i < kit.Cards.Length; i++)
            {
                var card = kit.Cards[i];
                lines.Add(LaneRoster.CardName(card) + "  " + LaneRoster.CardTag(card) + " — " + LaneRoster.CardLine(card));
            }
            body.text = string.Join("\n", lines);
            var width = _dossier.rect.width;
            if (width < 40f)
                width = 680f;
            body.rectTransform.sizeDelta = new Vector2(-20f, 10f);
            var preferred = body.preferredHeight + 28f;
            body.rectTransform.sizeDelta = new Vector2(-20f, preferred);
            _dossier.sizeDelta = new Vector2(0f, preferred + 112f);

            var picked = ShowPick.Yours.Contains(kit.Id);
            var full = !picked && ShowPick.Yours.Count >= ShowPick.Mode;
            var laneOnly = !kit.Linked && ShowPick.Mode <= 1;
            var add = Button(_dossier, picked ? "Remove from team" : laneOnly ? "Not in the lane yet" : full ? "Team is full" : "Add to team",
                new Vector2(0.04f, 1f), new Vector2(0.52f, 1f), !laneOnly && (picked || !full));
            var addRect = add.GetComponent<RectTransform>();
            addRect.pivot = new Vector2(0f, 1f);
            addRect.anchoredPosition = new Vector2(12f, -8f);
            addRect.sizeDelta = new Vector2(280f, 42f);
            add.interactable = !laneOnly && (picked || !full);
            var id = kit.Id;
            add.onClick.AddListener(() =>
            {
                if (ShowPick.Toggle(id))
                    Refresh();
            });
        }

        void Slot(string id)
        {
            var kit = LaneRoster.ById(id);
            var slot = Button(_tray, kit.Name ?? id, Vector2.zero, Vector2.one, false);
            var rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(168f, 0f);
            var layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 168f;
            layout.minWidth = 168f;
            slot.gameObject.AddComponent<RectMask2D>();
            var label = slot.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = kit.Name ?? id;
                label.fontSize = 15;
                label.resizeTextForBestFit = false;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }
            var drop = id;
            slot.onClick.AddListener(() =>
            {
                ShowPick.Focus(drop);
                ShowPick.Drop(drop);
                Refresh();
            });
        }

        static Sprite StillFor(string id)
        {
            var body = FighterBodies.Get(id);
            return body != null ? body.Idle : null;
        }

        IEnumerator WarmFace(string id)
        {
            yield return FighterBodies.LoadIdle(id);
            if (LaneRoster.ById(ShowPick.Inspect).Id != null)
                Refresh();
        }

        static void PaintFilter(string label, bool on)
        {
            var node = GameObject.Find(label);
            if (node == null)
                return;
            var button = node.GetComponent<Button>();
            if (button != null)
                PaintMode(button, on);
        }

        static void PaintMode(Button button, bool on)
        {
            var colors = button.colors;
            colors.normalColor = on ? Gold : Ink;
            colors.highlightedColor = on ? new Color(1f, 0.72f, 0.2f, 1f) : new Color(0.2f, 0.14f, 0.06f, 1f);
            colors.pressedColor = Gold;
            colors.selectedColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            colors.disabledColor = new Color(0.12f, 0.09f, 0.05f, 0.7f);
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.color = on ? Ink : Gold;
        }

        static string Names(List<string> ids)
        {
            if (ids.Count == 0)
                return "no one fielded";
            var parts = new string[ids.Count];
            for (var i = 0; i < ids.Count; i++)
            {
                var kit = LaneRoster.ById(ids[i]);
                parts[i] = kit.Name ?? ids[i];
            }
            return string.Join(", ", parts);
        }

        static string Clip(Text label, string line)
        {
            if (label == null || string.IsNullOrEmpty(line))
                return line ?? "";
            var width = label.rectTransform.rect.width;
            if (width < 24f || label.font == null)
                return line;
            var settings = label.GetGenerationSettings(new Vector2(width, label.fontSize + 6f));
            var gen = new TextGenerator();
            if (gen.GetPreferredWidth(line, settings) <= width + 0.5f)
                return line;
            var cut = line;
            while (cut.Length > 1)
            {
                cut = cut.Substring(0, cut.Length - 1).TrimEnd();
                var trial = cut + "…";
                if (gen.GetPreferredWidth(trial, settings) <= width + 0.5f)
                    return trial;
            }
            return "…";
        }

        static void WarmFont(Font font)
        {
            if (font == null)
                return;
            var text = new System.Text.StringBuilder(8192);
            text.Append("Team select FIGHT NOT READY Lane not ready no one fielded Add to team Remove from team ·…—");
            var all = LaneRoster.All;
            for (var i = 0; i < all.Length; i++)
            {
                var kit = all[i];
                text.Append(' ').Append(kit.Name).Append(' ').Append(kit.Tag).Append(' ').Append(kit.Bio).Append(' ').Append(kit.Counter).Append(' ').Append(kit.Atk);
            }
            var sample = text.ToString();
            int[] sizes = { 13, 15, 16, 18, 20, 22, 28 };
            for (var i = 0; i < sizes.Length; i++)
                font.RequestCharactersInTexture(sample, sizes[i], FontStyle.Bold);
        }

        static Color Hex(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return Gold;
        }

        static void Wipe(RectTransform root)
        {
            if (root == null)
                return;
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        RectTransform Scroll(Transform parent, string name, Vector2 min, Vector2 max, bool stack)
        {
            var root = PanelBox(parent, name, min, max);
            var viewport = Child(root, "Viewport");
            Stretch(viewport.GetComponent<RectTransform>());
            var maskImage = viewport.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.01f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var content = Child(viewport.transform, "Content");
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 400f);
            if (stack)
            {
                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 6f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return contentRect;
        }

        Transform PanelBox(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var box = Child(parent, name);
            var rect = box.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = box.AddComponent<Image>();
            image.color = Panel;
            image.raycastTarget = true;
            return box.transform;
        }

        Button Button(Transform parent, string text, Vector2 min, Vector2 max, bool gold)
        {
            var buttonObject = Child(parent, text);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var face = buttonObject.AddComponent<Image>();
            face.color = Color.white;
            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.fadeDuration = 0.08f;
            colors.colorMultiplier = 1f;
            colors.normalColor = gold ? Gold : Ink;
            colors.highlightedColor = colors.normalColor;
            colors.pressedColor = new Color(0xc4 / 255f, 0x74 / 255f, 0x08 / 255f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.15f, 0.11f, 0.06f, 0.65f);
            button.colors = colors;
            button.targetGraphic = face;
            var label = Words(buttonObject.transform, "Label", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), 20, TextAnchor.MiddleCenter);
            label.text = text;
            label.color = gold ? Ink : Gold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 22;
            return button;
        }

        Text Words(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor align)
        {
            var node = Child(parent, name);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = node.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = Paper;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static GameObject Child(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return node;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
