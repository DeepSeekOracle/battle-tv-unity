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

            var valeBar = Bar(canvasObject.transform, "ValeBar", new Vector2(0.03f, 0.9f), new Vector2(0.4f, 0.96f));
            var bruteBar = Bar(canvasObject.transform, "BruteBar", new Vector2(0.6f, 0.9f), new Vector2(0.97f, 0.96f));
            var valeText = Label(canvasObject.transform, "ValeRead", new Vector2(0.03f, 0.84f), new Vector2(0.48f, 0.9f), TextAnchor.MiddleLeft);
            var bruteText = Label(canvasObject.transform, "BruteRead", new Vector2(0.52f, 0.84f), new Vector2(0.97f, 0.9f), TextAnchor.MiddleRight);
            var callout = Label(canvasObject.transform, "Callout", new Vector2(0.18f, 0.78f), new Vector2(0.82f, 0.84f), TextAnchor.MiddleCenter);
            var result = Label(canvasObject.transform, "Result", new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.74f), TextAnchor.MiddleCenter);
            result.fontSize = 36;
            result.resizeTextMaxSize = 42;

            var menu = Button(canvasObject.transform, "MENU", new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.08f));
            var view = canvasObject.AddComponent<DuelView>();
            var so = new SerializedObject(view);
            so.FindProperty("plate").objectReferenceValue = plate;
            so.FindProperty("valeBody").objectReferenceValue = vale;
            so.FindProperty("bruteBody").objectReferenceValue = brute;
            so.FindProperty("valeFill").objectReferenceValue = valeBar;
            so.FindProperty("bruteFill").objectReferenceValue = bruteBar;
            so.FindProperty("valeHp").objectReferenceValue = valeText;
            so.FindProperty("bruteHp").objectReferenceValue = bruteText;
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

        static Image Bar(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var back = new GameObject(name, typeof(RectTransform), typeof(Image));
            back.transform.SetParent(parent, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = min;
            backRect.anchorMax = max;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            back.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.8f);
            back.GetComponent<Image>().raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(back.transform, false);
            var rect = fill.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0.5f);
            var image = fill.GetComponent<Image>();
            image.color = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
            image.raycastTarget = false;
            return image;
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
