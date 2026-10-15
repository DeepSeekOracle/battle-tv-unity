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

            var valeHud = Hud(canvasObject.transform, "ValeHud", new Vector2(0.018f, 0.735f), new Vector2(0.40f, 0.975f), false,
                new Color(0.91f, 0.77f, 0.42f, 1f));
            var bruteHud = Hud(canvasObject.transform, "BruteHud", new Vector2(0.60f, 0.735f), new Vector2(0.982f, 0.975f), true,
                new Color(0.83f, 0.33f, 0.23f, 1f));
            var clock = Label(canvasObject.transform, "Clock", new Vector2(0.42f, 0.90f), new Vector2(0.58f, 0.975f), TextAnchor.MiddleCenter);
            clock.resizeTextMaxSize = 28;
            var callout = Label(canvasObject.transform, "Callout", new Vector2(0.22f, 0.675f), new Vector2(0.78f, 0.73f), TextAnchor.MiddleCenter);
            var result = Label(canvasObject.transform, "Result", new Vector2(0.2f, 0.56f), new Vector2(0.8f, 0.68f), TextAnchor.MiddleCenter);
            result.fontSize = 36;
            result.resizeTextMaxSize = 42;

            var menu = Button(canvasObject.transform, "MENU", new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.08f));
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

        static void Configure(string path, bool alpha)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Duel art is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
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

        static LaneHud Hud(Transform parent, string name, Vector2 min, Vector2 max, bool fromRight, Color healthColor)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = min;
            rootRect.anchorMax = max;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var plateImage = root.GetComponent<Image>();
            plateImage.color = new Color(0.08f, 0.05f, 0.03f, 0.82f);
            plateImage.raycastTarget = false;

            var rim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
            rim.transform.SetParent(root.transform, false);
            var rimRect = rim.GetComponent<RectTransform>();
            rimRect.anchorMin = Vector2.zero;
            rimRect.anchorMax = Vector2.one;
            rimRect.offsetMin = new Vector2(1f, 1f);
            rimRect.offsetMax = new Vector2(-1f, -1f);
            var rimImage = rim.GetComponent<Image>();
            rimImage.color = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 0.95f);
            rimImage.raycastTarget = false;
            var well = new GameObject("Well", typeof(RectTransform), typeof(Image));
            well.transform.SetParent(rim.transform, false);
            var wellRect = well.GetComponent<RectTransform>();
            wellRect.anchorMin = Vector2.zero;
            wellRect.anchorMax = Vector2.one;
            wellRect.offsetMin = new Vector2(2f, 2f);
            wellRect.offsetMax = new Vector2(-2f, -2f);
            var wellImage = well.GetComponent<Image>();
            wellImage.color = new Color(0.07f, 0.05f, 0.03f, 0.94f);
            wellImage.raycastTarget = false;

            var host = well.transform;
            var title = Caption(host, "Title", new Vector2(0.04f, 0.78f), new Vector2(0.62f, 0.96f), fromRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            title.resizeTextMaxSize = 26;
            var cards = Caption(host, "Cards", new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.78f), fromRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            cards.color = new Color(0.84f, 0.75f, 0.58f, 1f);
            cards.resizeTextMaxSize = 13;
            var healthNum = Caption(host, "HealthNum", new Vector2(0.64f, 0.78f), new Vector2(0.96f, 0.96f), fromRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            var health = Track(host, "Health", new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.62f), healthColor);
            var manaNum = Caption(host, "ManaNum", new Vector2(0.78f, 0.30f), new Vector2(0.96f, 0.44f), fromRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            manaNum.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            var mana = Track(host, "Mana", new Vector2(0.04f, 0.30f), new Vector2(0.76f, 0.44f), new Color(0.22f, 0.74f, 0.97f, 1f));
            var stam = Track(host, "Stam", new Vector2(0.04f, 0.08f), new Vector2(0.34f, 0.26f), new Color(0.91f, 0.69f, 0.35f, 1f));
            var block = Track(host, "Block", new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.26f), new Color(0.84f, 0.87f, 0.91f, 1f));
            var dash = Track(host, "Dash", new Vector2(0.66f, 0.08f), new Vector2(0.96f, 0.26f), new Color(0.98f, 0.75f, 0.14f, 1f));
            var healthWord = Caption(host, "HealthWord", new Vector2(0.05f, 0.46f), new Vector2(0.28f, 0.62f), TextAnchor.MiddleLeft);
            healthWord.text = "HEALTH";
            var manaWord = Caption(host, "ManaWord", new Vector2(0.05f, 0.30f), new Vector2(0.24f, 0.44f), TextAnchor.MiddleLeft);
            manaWord.text = "MANA";
            var stamWord = Caption(host, "StamWord", new Vector2(0.04f, 0.08f), new Vector2(0.34f, 0.26f), TextAnchor.MiddleCenter);
            stamWord.text = "STAM";
            var blockNum = Caption(host, "BlockNum", new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.26f), TextAnchor.MiddleCenter);
            blockNum.text = "BLOCK";
            var dashNum = Caption(host, "DashNum", new Vector2(0.66f, 0.08f), new Vector2(0.96f, 0.26f), TextAnchor.MiddleCenter);
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

        static Image Track(Transform parent, string name, Vector2 min, Vector2 max, Color fillColor)
        {
            var track = new GameObject(name, typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = min;
            trackRect.anchorMax = max;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            var back = track.GetComponent<Image>();
            back.color = new Color(0.03f, 0.02f, 0.015f, 1f);
            back.raycastTarget = false;
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);
            var fill = fillObject.GetComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;
            var glossObject = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
            glossObject.transform.SetParent(fillObject.transform, false);
            var glossRect = glossObject.GetComponent<RectTransform>();
            glossRect.anchorMin = new Vector2(0f, 0.55f);
            glossRect.anchorMax = Vector2.one;
            glossRect.offsetMin = Vector2.zero;
            glossRect.offsetMax = Vector2.zero;
            var gloss = glossObject.GetComponent<Image>();
            gloss.color = new Color(1f, 0.95f, 0.82f, 0.28f);
            gloss.raycastTarget = false;
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

        static Button Button(Transform parent, string caption, Vector2 min, Vector2 max)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var face = go.GetComponent<Image>();
            face.color = Color.white;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
            colors.highlightedColor = new Color(1f, 0.72f, 0.18f, 1f);
            colors.pressedColor = new Color(0xc4 / 255f, 0x74 / 255f, 0x08 / 255f, 1f);
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
