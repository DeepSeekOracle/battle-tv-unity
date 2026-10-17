using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleTv.Menu.EditorTools
{
    public static class TeamSelectSetup
    {
        const string ScenePath = "Assets/Menu/TeamSelect.unity";
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

            var fit = canvasObject.AddComponent<MenuController>();
            var fitObject = new SerializedObject(fit);
            fitObject.FindProperty("plate").objectReferenceValue = plate;
            fitObject.FindProperty("restImage").objectReferenceValue = restPair.image;
            fitObject.FindProperty("rest").objectReferenceValue = restPair.player;
            fitObject.ApplyModifiedPropertiesWithoutUndo();

            var team = canvasObject.AddComponent<TeamSelectController>();
            var teamObject = new SerializedObject(team);
            teamObject.FindProperty("valeStill").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/vale.png");
            teamObject.FindProperty("bruteStill").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute.png");
            ArenaSceneSetup.ImportFatherStill();
            var father = ArenaSceneSetup.LoadPlate("Assets/Art/Duel/lightfather.png");
            if (father == null)
                throw new FileNotFoundException("Lightfather still did not import.", "Assets/Art/Duel/lightfather.png");
            teamObject.FindProperty("lightfatherStill").objectReferenceValue = father;
            ArenaSceneSetup.ImportKael();
            var kael = ArenaSceneSetup.LoadPlate("Assets/Art/Duel/kael.png");
            if (kael == null)
                throw new FileNotFoundException("Kael still did not import.", "Assets/Art/Duel/kael.png");
            teamObject.FindProperty("kaelStill").objectReferenceValue = kael;
            teamObject.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Menu/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/GameMenu.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Fight/Duel.unity", true),
                new EditorBuildSettingsScene("Assets/Fight/Arena.unity", true)
            };
            AssetDatabase.SaveAssets();
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
