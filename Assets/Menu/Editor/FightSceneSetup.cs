using System.IO;
using BattleTv.Fight;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Menu.EditorTools
{
    public static class FightSceneSetup
    {
        const string ScenePath = "Assets/Fight/Duel.unity";
        const string StagePath = "Assets/Art/Duel/stage.jpg";
        const string ValePath = "Assets/Art/Duel/vale.png";
        const string BrutePath = "Assets/Art/Duel/brute.png";

        public static void Build()
        {
            Configure(StagePath, false);
            Configure(ValePath, true);
            Configure(BrutePath, true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var chrome = canvasObject.AddComponent<PageChrome>();
            var chromeObject = new SerializedObject(chrome);
            chromeObject.FindProperty("screen").stringValue = "duel";
            chromeObject.ApplyModifiedPropertiesWithoutUndo();

            var plateObject = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plateObject.transform.SetParent(canvasObject.transform, false);
            var plate = plateObject.GetComponent<RectTransform>();
            var stage = plateObject.GetComponent<Image>();
            stage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StagePath);
            stage.preserveAspect = false;
            stage.raycastTarget = false;
            if (stage.sprite == null)
                throw new FileNotFoundException("Duel stage did not import.", StagePath);

            var vale = Fighter(plate, "Vale", ValePath);
            var brute = Fighter(plate, "Brute", BrutePath);

            var bar = Skin("bar-frame.png");
            var pip = Skin("pip-frame.png");
            var plaque = Skin("plaque.png");
            var buttonSkin = Skin("button.png");
            var healthFill = Skin("fill-health.png");
            var manaFill = Skin("fill-mana.png");
            var stamFill = Skin("fill-stam.png");
            var blockFill = Skin("fill-block.png");
            var dashFill = Skin("fill-dash.png");
            var valeHud = Hud(canvasObject.transform, "ValeHud", new Vector2(0.012f, 0.64f), new Vector2(0.40f, 0.985f), false,
                plaque, bar, pip, healthFill, manaFill, stamFill, blockFill, dashFill);
            var bruteHud = Hud(canvasObject.transform, "BruteHud", new Vector2(0.60f, 0.64f), new Vector2(0.988f, 0.985f), true,
                plaque, bar, pip, healthFill, manaFill, stamFill, blockFill, dashFill);
            var clock = Label(canvasObject.transform, "Clock", new Vector2(0.42f, 0.90f), new Vector2(0.58f, 0.975f), TextAnchor.MiddleCenter);
            clock.resizeTextMaxSize = 28;
            var callout = Label(canvasObject.transform, "Callout", new Vector2(0.28f, 0.575f), new Vector2(0.72f, 0.635f), TextAnchor.MiddleCenter);
            var result = Label(canvasObject.transform, "Result", new Vector2(0.2f, 0.46f), new Vector2(0.8f, 0.57f), TextAnchor.MiddleCenter);
            result.fontSize = 36;
            result.resizeTextMaxSize = 42;

            var menu = Button(canvasObject.transform, "MENU", new Vector2(0.016f, 0.02f), new Vector2(0.145f, 0.095f), buttonSkin);
            var view = canvasObject.AddComponent<DuelView>();
            var so = new SerializedObject(view);
            so.FindProperty("plate").objectReferenceValue = plate;
            so.FindProperty("valeBody").objectReferenceValue = vale;
            so.FindProperty("bruteBody").objectReferenceValue = brute;
            so.FindProperty("valeHud").objectReferenceValue = valeHud;
            so.FindProperty("bruteHud").objectReferenceValue = bruteHud;
            so.FindProperty("clock").objectReferenceValue = clock;
            so.FindProperty("callout").objectReferenceValue = callout;
            so.FindProperty("result").objectReferenceValue = result;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(menu.onClick, view.BackToMenu);

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(menu.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Menu/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/GameMenu.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            Debug.Log("[FightSceneSetup] Duel scene saved.");
        }

        static void Configure(string path, bool alpha, bool ui = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Duel art is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            if (ui)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 1024;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static RectTransform Fighter(Transform parent, string name, string path)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null)
                throw new FileNotFoundException("Fighter did not import.", path);
            return go.GetComponent<RectTransform>();
        }

        static Sprite Skin(string file)
        {
            var path = "Assets/Art/Duel/ui/" + file;
            Configure(path, true, true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException("HUD skin did not import.", path);
            return sprite;
        }

        static LaneHud Hud(Transform parent, string name, Vector2 min, Vector2 max, bool fromRight, Sprite plaque, Sprite bar, Sprite pip, Sprite healthFill, Sprite manaFill, Sprite stamFill, Sprite blockFill, Sprite dashFill)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = min;
            rootRect.anchorMax = max;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var plateImage = root.GetComponent<Image>();
            plateImage.sprite = plaque;
            plateImage.type = Image.Type.Simple;
            plateImage.color = Color.white;
            plateImage.raycastTarget = false;

            var host = root.transform;
            var title = Caption(host, "Title", new Vector2(0.06f, 0.84f), new Vector2(0.62f, 0.98f), fromRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            title.resizeTextMaxSize = 26;
            var cards = Caption(host, "Cards", new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.84f), fromRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            cards.color = new Color(0.84f, 0.75f, 0.58f, 1f);
            cards.resizeTextMaxSize = 13;
            var healthNum = Caption(host, "HealthNum", new Vector2(0.64f, 0.84f), new Vector2(0.94f, 0.98f), fromRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            var health = Track(host, "Health", new Vector2(0.045f, 0.52f), new Vector2(0.955f, 0.72f), healthFill, bar);
            var manaNum = Caption(host, "ManaNum", new Vector2(0.78f, 0.36f), new Vector2(0.96f, 0.50f), fromRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            manaNum.color = new Color(0.73f, 0.9f, 0.98f, 1f);
            var mana = Track(host, "Mana", new Vector2(0.045f, 0.36f), new Vector2(0.76f, 0.50f), manaFill, bar);
            var stam = Track(host, "Stam", new Vector2(0.05f, 0.05f), new Vector2(0.195f, 0.34f), stamFill, pip);
            var block = Track(host, "Block", new Vector2(0.40f, 0.05f), new Vector2(0.545f, 0.34f), blockFill, pip);
            var dash = Track(host, "Dash", new Vector2(0.78f, 0.05f), new Vector2(0.925f, 0.34f), dashFill, pip);
            var healthWord = Caption(host, "HealthWord", new Vector2(0.07f, 0.52f), new Vector2(0.28f, 0.72f), TextAnchor.MiddleLeft);
            healthWord.text = "HEALTH";
            var manaWord = Caption(host, "ManaWord", new Vector2(0.07f, 0.36f), new Vector2(0.26f, 0.50f), TextAnchor.MiddleLeft);
            manaWord.text = "MANA";
            var stamWord = Caption(host, "StamWord", new Vector2(0.05f, 0.05f), new Vector2(0.195f, 0.34f), TextAnchor.MiddleCenter);
            stamWord.text = "STAM";
            var blockNum = Caption(host, "BlockNum", new Vector2(0.40f, 0.05f), new Vector2(0.545f, 0.34f), TextAnchor.MiddleCenter);
            blockNum.text = "BLOCK";
            var dashNum = Caption(host, "DashNum", new Vector2(0.78f, 0.05f), new Vector2(0.925f, 0.34f), TextAnchor.MiddleCenter);
            dashNum.text = "DASH";

            var hud = root.AddComponent<LaneHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("health").objectReferenceValue = health;
            so.FindProperty("mana").objectReferenceValue = mana;
            so.FindProperty("stam").objectReferenceValue = stam;
            so.FindProperty("block").objectReferenceValue = block;
            so.FindProperty("dash").objectReferenceValue = dash;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("cards").objectReferenceValue = cards;
            so.FindProperty("healthNum").objectReferenceValue = healthNum;
            so.FindProperty("manaNum").objectReferenceValue = manaNum;
            so.FindProperty("blockNum").objectReferenceValue = blockNum;
            so.FindProperty("dashNum").objectReferenceValue = dashNum;
            so.FindProperty("fromRight").boolValue = fromRight;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        static Image Track(Transform parent, string name, Vector2 min, Vector2 max, Sprite fillSprite, Sprite frame)
        {
            var track = new GameObject(name, typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = min;
            trackRect.anchorMax = max;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            var back = track.GetComponent<Image>();
            back.color = new Color(0.04f, 0.03f, 0.02f, 0.92f);
            back.raycastTarget = false;
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);
            var fill = fillObject.GetComponent<Image>();
            fill.sprite = fillSprite;
            fill.type = Image.Type.Simple;
            fill.color = Color.white;
            fill.raycastTarget = false;
            var coverObject = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            coverObject.transform.SetParent(track.transform, false);
            var coverRect = coverObject.GetComponent<RectTransform>();
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = Vector2.zero;
            coverRect.offsetMax = Vector2.zero;
            var cover = coverObject.GetComponent<Image>();
            cover.sprite = frame;
            cover.type = Image.Type.Simple;
            cover.preserveAspect = false;
            cover.color = Color.white;
            cover.raycastTarget = false;
            return fill;
        }

        static Text Caption(Transform parent, string name, Vector2 min, Vector2 max, TextAnchor align)
        {
            var text = Label(parent, name, min, max, align);
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 16;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        static Text Label(Transform parent, string name, Vector2 min, Vector2 max, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 16;
            text.alignment = align;
            text.color = new Color(0xf6 / 255f, 0xe7 / 255f, 0xc1 / 255f, 1f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 22;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static Button Button(Transform parent, string caption, Vector2 min, Vector2 max, Sprite skin)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var face = go.GetComponent<Image>();
            face.sprite = skin;
            face.type = Image.Type.Simple;
            face.color = Color.white;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 1f);
            colors.pressedColor = new Color(0.75f, 0.68f, 0.55f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0x1a / 255f, 0x12 / 255f, 0x06 / 255f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
            return button;
        }
    }
}
