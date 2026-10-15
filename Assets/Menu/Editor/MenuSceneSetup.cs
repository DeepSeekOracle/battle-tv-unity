using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BattleTv.Menu.EditorTools
{
    public static class MenuSceneSetup
    {
        const string PlatePath = "Assets/Art/Menu/Vale-vs-Brute.jpg";
        const string ScenePath = "Assets/Menu/Menu.unity";

        public static void Build()
        {
            ConfigurePlate();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuSceneSetup] Menu scene saved.");
        }

        static void ConfigurePlate()
        {
            var importer = AssetImporter.GetAtPath(PlatePath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Menu plate is missing.", PlatePath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.readable = false;
            importer.SetTextureSettings(settings);

            SetUncompressed(importer, "DefaultTexturePlatform", overridden: false);
            SetUncompressed(importer, "Standalone", overridden: true);
            SetUncompressed(importer, "WebGL", overridden: true);
            importer.SaveAndReimport();
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.orthographic = true;
            cameraObject.AddComponent<AudioListener>();
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
            still.color = Color.white;
            if (still.sprite == null)
                throw new FileNotFoundException("Menu plate sprite did not import.", PlatePath);

            var clipObject = new GameObject("Clip", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
            clipObject.transform.SetParent(plate, false);
            Stretch(clipObject.GetComponent<RectTransform>());
            var raw = clipObject.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;
            var player = clipObject.GetComponent<VideoPlayer>();
            player.source = VideoSource.Url;
            player.isLooping = false;
            player.playOnAwake = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.aspectRatio = VideoAspectRatio.Stretch;

            var buttonObject = new GameObject("BattleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(plate, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            // Lower centre of the sand. Sized as a fraction of the plate so it
            // stays off the kite, the club, and both faces, and stays on screen
            // when a 21:9 window crops the top and bottom.
            buttonRect.anchorMin = new Vector2(0.414f, 0.125f);
            buttonRect.anchorMax = new Vector2(0.586f, 0.203f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            var face = buttonObject.GetComponent<Image>();
            face.color = Color.white;
            face.raycastTarget = true;
            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Hex(0xf5, 0x9e, 0x0b);
            colors.highlightedColor = Hex(0xff, 0xb0, 0x2e);
            colors.pressedColor = Hex(0xc4, 0x74, 0x08);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.targetGraphic = face;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            Stretch(labelObject.GetComponent<RectTransform>());
            var label = labelObject.GetComponent<Text>();
            label.text = "BATTLE TV";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 64;
            label.color = Hex(0x1a, 0x12, 0x06);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var launch = canvasObject.AddComponent<NoShow>();
            var controller = canvasObject.AddComponent<MenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("plate").objectReferenceValue = plate;
            so.FindProperty("videoImage").objectReferenceValue = raw;
            so.FindProperty("video").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();

            var click = new UnityEngine.Events.UnityAction(launch.RequestShow);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, click);

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(buttonObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
                AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetUncompressed(TextureImporter importer, string target, bool overridden)
        {
            var platform = target == "DefaultTexturePlatform"
                ? importer.GetDefaultPlatformTextureSettings()
                : importer.GetPlatformTextureSettings(target);
            platform.name = target;
            platform.overridden = overridden;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            platform.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(platform);
        }

        static Color Hex(byte r, byte g, byte b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
