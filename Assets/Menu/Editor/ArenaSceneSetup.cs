using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BattleTv.Fight;

namespace BattleTv.Menu.EditorTools
{
    public static class ArenaSceneSetup
    {
        public const string ScenePath = "Assets/Fight/Arena.unity";
        const string PitPath = "Assets/Art/Arena/pit.jpg";
        const string MarkPath = "Assets/Art/Arena/mark.png";

        public static void Build()
        {
            Configure(PitPath, false, 2048);
            Configure(MarkPath, true, 64);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var plateObject = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plateObject.transform.SetParent(canvasObject.transform, false);
            var plate = plateObject.GetComponent<RectTransform>();
            var floor = plateObject.GetComponent<Image>();
            floor.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PitPath);
            floor.preserveAspect = false;
            floor.raycastTarget = false;
            if (floor.sprite == null)
                throw new FileNotFoundException("Arena pit did not import.", PitPath);

            var view = canvasObject.AddComponent<ArenaView>();
            var so = new SerializedObject(view);
            so.FindProperty("plate").objectReferenceValue = plate;
            so.FindProperty("valeStill").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/vale.png");
            so.FindProperty("bruteStill").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute.png");
            so.FindProperty("mark").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(MarkPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Menu/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/GameMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/TeamSelect.unity", true),
                new EditorBuildSettingsScene("Assets/Fight/Duel.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[ArenaSceneSetup] Arena scene saved.");
        }

        static void Configure(string path, bool alpha, int max)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Arena art is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.textureCompression = alpha ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = max;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
