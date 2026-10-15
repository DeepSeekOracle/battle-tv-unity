using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleTv.Menu.EditorTools
{
    public static class GameMenuSetup
    {
        const string ScenePath = "Assets/Menu/GameMenu.unity";
        const string PlatePath = "Assets/Art/Menu/Vale-vs-Brute.jpg";

        public static void Build()
        {
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

            var plateObject = new GameObject("Plate", typeof(RectTransform));
            plateObject.transform.SetParent(canvasObject.transform, false);
            var plate = plateObject.GetComponent<RectTransform>();

            var stillObject = new GameObject("Still", typeof(RectTransform), typeof(Image));
            stillObject.transform.SetParent(plate, false);
            Stretch(stillObject.GetComponent<RectTransform>());
            var still = stillObject.GetComponent<Image>();
            still.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlatePath);
            still.preserveAspect = false;
            still.raycastTarget = false;
            if (still.sprite == null)
                throw new FileNotFoundException("Menu plate sprite did not import.", PlatePath);

            var restPair = MenuSceneSetup.AddVideo(plate, "Rest", looping: true);
            var chrome = canvasObject.AddComponent<PageChrome>();
            var chromeObject = new SerializedObject(chrome);
            chromeObject.FindProperty("screen").stringValue = "menu";
            chromeObject.ApplyModifiedPropertiesWithoutUndo();

            var menu = canvasObject.AddComponent<GameMenuController>();
            var fit = canvasObject.AddComponent<MenuController>();
            var fitObject = new SerializedObject(fit);
            fitObject.FindProperty("plate").objectReferenceValue = plate;
            fitObject.FindProperty("restImage").objectReferenceValue = restPair.image;
            fitObject.FindProperty("rest").objectReferenceValue = restPair.player;
            fitObject.ApplyModifiedPropertiesWithoutUndo();

            var bar = new GameObject("MenuBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(canvasObject.transform, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.18f, 0.06f);
            barRect.anchorMax = new Vector2(0.82f, 0.34f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            var shade = bar.GetComponent<Image>();
            shade.color = new Color(0.04f, 0.03f, 0.02f, 0.62f);
            shade.raycastTarget = false;

            var values = new[] { 1, 2, 3, 5 };
            var labels = new[] { "1v1", "2v2", "3v3", "5v5" };
            var buttons = new Button[4];
            for (var i = 0; i < 4; i++)
            {
                var x0 = 0.06f + i * 0.235f;
                buttons[i] = MakeButton(bar.transform, labels[i], new Vector2(x0, 0.52f), new Vector2(x0 + 0.21f, 0.88f), false);
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, menu.Pick, values[i]);
            }

            var start = MakeButton(bar.transform, "BATTLE TV", new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.42f), true);
            var gate = canvasObject.AddComponent<DuelGate>();
            UnityEventTools.AddPersistentListener(start.onClick, gate.OpenCard);

            var menuObject = new SerializedObject(menu);
            menuObject.FindProperty("modes").arraySize = 4;
            menuObject.FindProperty("modeValues").arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                menuObject.FindProperty("modes").GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                menuObject.FindProperty("modeValues").GetArrayElementAtIndex(i).intValue = values[i];
            }
            menuObject.ApplyModifiedPropertiesWithoutUndo();

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(start.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Menu/Menu.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        static Button MakeButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, bool gold)
        {
            var buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var face = buttonObject.GetComponent<Image>();
            face.type = Image.Type.Simple;
            face.color = Color.white;
            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            colors.normalColor = gold
                ? new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f)
                : new Color(0x1a / 255f, 0x12 / 255f, 0x06 / 255f, 1f);
            colors.highlightedColor = colors.normalColor;
            colors.pressedColor = new Color(0xc4 / 255f, 0x74 / 255f, 0x08 / 255f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            button.targetGraphic = face;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            Stretch(labelObject.GetComponent<RectTransform>());
            var label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = gold
                ? new Color(0x1a / 255f, 0x12 / 255f, 0x06 / 255f, 1f)
                : new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 40;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.06f, 0.1f);
            labelRect.anchorMax = new Vector2(0.94f, 0.9f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
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
