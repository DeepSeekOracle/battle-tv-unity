using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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
            Configure(StagePath, false, false, Vector4.zero, true);
            Configure(ValePath, true);
            Configure(BrutePath, true);
            var valeToss = LoadClip("vale-toss", 8);
            var valeBash = LoadClip("vale-bash", 6);
            var valePunch = LoadClip("vale-punch", 6);
            var valeLine = LoadClip("vale-line", 5);
            var valePlant = LoadClip("vale-plant", 4);
            var valeDeath = LoadClip("vale-death", 4, 1024);
            var valeWin = LoadClip("vale-win", 2, 1024);
            Configure("Assets/Art/Duel/brute-drag.png", true);
            var bruteSweep = LoadClip("brute-sweep", 8, 1024);
            var brutePound = LoadClip("brute-pound", 2, 1024);
            var bruteWard = LoadClip("brute-ward", 2, 1024);
            var bruteClose = LoadClip("brute-close", 1, 1024);
            var bruteDeath = LoadClip("brute-death", 3, 1024);
            var bruteWin = LoadClip("brute-win", 2, 1024);
            ConfigureToss("Assets/Art/Duel/brute-line.png", 1024);
            ConfigureToss("Assets/Art/Duel/brute-grip.png", 1024);
            ConfigureToss("Assets/Art/Duel/brute-dust.png", 1024);
            ApplyBrutePivots();
            bruteSweep = ReloadClip("brute-sweep", 8);
            brutePound = ReloadClip("brute-pound", 2);
            bruteWard = ReloadClip("brute-ward", 2);
            bruteClose = ReloadClip("brute-close", 1);
            bruteDeath = ReloadClip("brute-death", 3);
            bruteWin = ReloadClip("brute-win", 2);
            var bruteIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-drag.png");
            var lineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-line.png");
            var gripSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-grip.png");
            var dustSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-dust.png");
            if (bruteIdle == null || lineSprite == null || gripSprite == null || dustSprite == null)
                throw new FileNotFoundException("Brute art did not import.", "Assets/Art/Duel/brute-drag.png");
            ConfigureToss("Assets/Art/Duel/vale-door/shield.png", 1024);
            ConfigureToss("Assets/Art/Duel/vale-door/glow-white.png", 64);
            var doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/vale-door/shield.png");
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/vale-door/glow-white.png");
            var openHand = valeToss[7];
            if (glowSprite == null)
                throw new FileNotFoundException("Ground ring sprite did not import.", "Assets/Art/Duel/vale-door/glow-white.png");
            var ringBackMat = RingMat("Assets/Fight/GroundRingBack.mat", 1f);
            var ringFrontMat = RingMat("Assets/Fight/GroundRingFront.mat", -1f);
            var sparkMat = SparkMat("Assets/Fight/HitSpark.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

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
            var brute = Fighter(plate, "Brute", "Assets/Art/Duel/brute-drag.png");

            var bar = Skin("bar-frame.png", new Vector4(12, 5, 12, 5));
            var pip = Skin("pip-frame.png", new Vector4(8, 4, 8, 4));
            var buttonSkin = Skin("button.png", new Vector4(10, 8, 10, 8));
            var healthFill = Skin("fill-health.png", Vector4.zero);
            var manaFill = Skin("fill-mana.png", Vector4.zero);
            var stamFill = Skin("fill-stam.png", Vector4.zero);
            var blockFill = Skin("fill-block.png", Vector4.zero);
            var dashFill = Skin("fill-dash.png", Vector4.zero);
            var valeHud = Hud(canvasObject.transform, "ValeHud", new Vector2(0.012f, 0.825f), new Vector2(0.40f, 0.985f), false,
                bar, pip, healthFill, manaFill, stamFill, blockFill, dashFill);
            var bruteHud = Hud(canvasObject.transform, "BruteHud", new Vector2(0.60f, 0.825f), new Vector2(0.988f, 0.985f), true,
                bar, pip, healthFill, manaFill, stamFill, blockFill, dashFill);
            var clockPlate = new GameObject("ClockPlate", typeof(RectTransform), typeof(Image));
            clockPlate.transform.SetParent(canvasObject.transform, false);
            var clockRect = clockPlate.GetComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(0.445f, 0.915f);
            clockRect.anchorMax = new Vector2(0.555f, 0.965f);
            clockRect.offsetMin = Vector2.zero;
            clockRect.offsetMax = Vector2.zero;
            var clockFace = clockPlate.GetComponent<Image>();
            clockFace.sprite = bar;
            clockFace.type = Image.Type.Sliced;
            clockFace.color = Color.white;
            clockFace.raycastTarget = false;
            var clock = Label(clockPlate.transform, "Clock", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            clock.resizeTextMaxSize = 14;
            var callout = Label(canvasObject.transform, "Callout", new Vector2(0.42f, 0.872f), new Vector2(0.58f, 0.908f), TextAnchor.MiddleCenter);
            callout.resizeTextMaxSize = 13;
            var result = Label(canvasObject.transform, "Result", new Vector2(0.2f, 0.46f), new Vector2(0.8f, 0.57f), TextAnchor.MiddleCenter);
            result.fontSize = 36;
            result.resizeTextMaxSize = 42;

            var menu = Button(canvasObject.transform, "MENU", new Vector2(0.016f, 0.018f), new Vector2(0.112f, 0.072f), buttonSkin);
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
            AssignClip(so, "valeToss", valeToss);
            AssignClip(so, "valeBash", valeBash);
            AssignClip(so, "valePunch", valePunch);
            AssignClip(so, "valeLine", valeLine);
            AssignClip(so, "valePlant", valePlant);
            AssignClip(so, "bruteSweep", bruteSweep);
            AssignClip(so, "brutePound", brutePound);
            AssignClip(so, "bruteWard", bruteWard);
            AssignClip(so, "bruteClose", bruteClose);
            AssignClip(so, "valeDeath", valeDeath);
            AssignClip(so, "valeWin", valeWin);
            AssignClip(so, "bruteDeath", bruteDeath);
            AssignClip(so, "bruteWin", bruteWin);
            so.FindProperty("bruteIdle").objectReferenceValue = bruteIdle;
            var door = Mark(plate, "ValeDoor", doorSprite);
            door.transform.SetSiblingIndex(3);
            so.FindProperty("valeDoor").objectReferenceValue = door;
            // Far half is the first plate child, so it draws behind both bodies.
            // Near half is last, so it draws over their feet. The shader clips the seam.
            var ringBack = Mark(plate, "ValeRingBack", glowSprite);
            var ringFront = Mark(plate, "ValeRingFront", glowSprite);
            ringBack.material = ringBackMat;
            ringFront.material = ringFrontMat;
            ringBack.transform.SetSiblingIndex(0);
            ringFront.transform.SetAsLastSibling();
            var spark = Mark(plate, "HitSpark", glowSprite);
            spark.material = sparkMat;
            spark.transform.SetAsLastSibling();
            so.FindProperty("valeRingBack").objectReferenceValue = ringBack;
            so.FindProperty("valeRing").objectReferenceValue = ringFront;
            so.FindProperty("hitSpark").objectReferenceValue = spark;
            so.FindProperty("valeOpen").objectReferenceValue = openHand;
            var poundLine = Mark(plate, "BrutePound", lineSprite);
            var gripRocks = Mark(plate, "BruteGrip", gripSprite);
            var dash = Mark(plate, "BruteDash", dustSprite);
            so.FindProperty("poundLine").objectReferenceValue = poundLine;
            so.FindProperty("gripRocks").objectReferenceValue = gripRocks;
            so.FindProperty("dashDust").objectReferenceValue = dash;
            var film = MenuSceneSetup.AddVideo(plate, "StageFilm", true);
            film.image.color = Color.white;
            so.FindProperty("stageFilm").objectReferenceValue = film.image;
            so.FindProperty("stagePlayer").objectReferenceValue = film.player;
            film.image.transform.SetSiblingIndex(0);
            ringBack.transform.SetSiblingIndex(1);
            poundLine.transform.SetSiblingIndex(2);
            vale.SetSiblingIndex(3);
            brute.SetSiblingIndex(4);
            door.transform.SetSiblingIndex(5);
            gripRocks.transform.SetSiblingIndex(6);
            dash.transform.SetSiblingIndex(7);
            ringFront.transform.SetSiblingIndex(8);
            spark.transform.SetAsLastSibling();
            var board = new GameObject("Scoreboard", typeof(RectTransform), typeof(Image));
            board.transform.SetParent(canvasObject.transform, false);
            var boardRect = board.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.28f, 0.30f);
            boardRect.anchorMax = new Vector2(0.72f, 0.64f);
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            var boardPlate = board.GetComponent<Image>();
            boardPlate.color = new Color(0.05f, 0.035f, 0.025f, 0.94f);
            boardPlate.raycastTarget = true;
            var boardFrame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            boardFrame.transform.SetParent(board.transform, false);
            var frameRect = boardFrame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            var frameImage = boardFrame.GetComponent<Image>();
            frameImage.sprite = bar;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;
            var scoreTitle = Label(board.transform, "Title", new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.92f), TextAnchor.MiddleCenter);
            scoreTitle.fontSize = 32;
            scoreTitle.resizeTextMaxSize = 36;
            var scoreDetail = Label(board.transform, "Detail", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.64f), TextAnchor.MiddleCenter);
            scoreDetail.fontSize = 18;
            scoreDetail.resizeTextMaxSize = 22;
            var replay = Button(board.transform, "REPLAY", new Vector2(0.08f, 0.10f), new Vector2(0.46f, 0.36f), buttonSkin);
            var toons = Button(board.transform, "NEW TOONS", new Vector2(0.54f, 0.10f), new Vector2(0.92f, 0.36f), buttonSkin);
            replay.GetComponentInChildren<Text>().fontSize = 18;
            toons.GetComponentInChildren<Text>().fontSize = 18;
            so.FindProperty("scoreboard").objectReferenceValue = board;
            so.FindProperty("scoreTitle").objectReferenceValue = scoreTitle;
            so.FindProperty("scoreDetail").objectReferenceValue = scoreDetail;
            so.ApplyModifiedPropertiesWithoutUndo();
            board.SetActive(false);
            UnityEventTools.AddPersistentListener(menu.onClick, view.BackToMenu);
            UnityEventTools.AddPersistentListener(replay.onClick, view.Replay);
            UnityEventTools.AddPersistentListener(toons.onClick, view.PickNew);

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(menu.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Menu/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/GameMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Menu/TeamSelect.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            Debug.Log("[FightSceneSetup] Duel scene saved.");
        }

        static void Configure(string path, bool alpha, bool ui = false, Vector4 border = default, bool photo = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Duel art is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.spriteBorder = border;
            if (ui)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.filterMode = border == Vector4.zero ? FilterMode.Bilinear : FilterMode.Point;
            }
            else if (photo)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static Material SparkMat(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Fight/HitSpark.shader");
            if (shader == null)
                throw new FileNotFoundException("Hit spark shader is missing.", "Assets/Fight/HitSpark.shader");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
                mat.shader = shader;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material RingMat(string path, float half)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Fight/GroundRing.shader");
            if (shader == null)
                throw new FileNotFoundException("Ground ring shader is missing.", "Assets/Fight/GroundRing.shader");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.SetFloat("_Half", half);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Image Mark(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        static Sprite[] ReloadClip(string folder, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var path = "Assets/Art/Duel/" + folder + "/f" + i.ToString("00") + ".png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                    throw new FileNotFoundException("Brute clip frame did not import.", path);
            }
            return frames;
        }

        static Sprite[] LoadClip(string folder, int count, int maxSize = 384)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var path = "Assets/Art/Duel/" + folder + "/f" + i.ToString("00") + ".png";
                ConfigureToss(path, maxSize);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                    throw new FileNotFoundException("Vale clip frame did not import.", path);
            }
            return frames;
        }

        static void ApplyBrutePivots()
        {
            var text = File.ReadAllText("Assets/Art/Duel/brute-pivots.json");
            foreach (Match match in Regex.Matches(text, "\"([^\"]+)\"\\s*:\\s*([0-9.]+)"))
            {
                string path = "Assets/Art/Duel/" + match.Groups[1].Value;
                float x = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new FileNotFoundException("Brute pivot target is missing.", path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(Mathf.Clamp01(x), 0f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        static void AssignClip(SerializedObject so, string property, Sprite[] frames)
        {
            var prop = so.FindProperty(property);
            prop.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        static void ConfigureToss(string path, int maxSize = 384)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Vale toss frame is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maxSize;
            importer.filterMode = FilterMode.Bilinear;
            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = maxSize;
            platform.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(platform);
            var web = importer.GetPlatformTextureSettings("WebGL");
            web.overridden = true;
            web.maxTextureSize = maxSize;
            web.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(web);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            if (path.EndsWith("vale-door/shield.png"))
            {
                // The old file was a torn slice. This kite is the whole shield from her toss frame.
                var whole = new SpriteMetaData
                {
                    name = "shield_0",
                    rect = new Rect(0f, 0f, 152f, 176f),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.02f)
                };
                importer.spritesheet = new[] { whole };
            }
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

        static Sprite Skin(string file, Vector4 border)
        {
            var path = "Assets/Art/Duel/ui/" + file;
            Configure(path, true, true, border);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException("HUD skin did not import.", path);
            return sprite;
        }

        static LaneHud Hud(Transform parent, string name, Vector2 min, Vector2 max, bool fromRight, Sprite bar, Sprite pip, Sprite healthFill, Sprite manaFill, Sprite stamFill, Sprite blockFill, Sprite dashFill)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = min;
            rootRect.anchorMax = max;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var plateImage = root.GetComponent<Image>();
            plateImage.color = new Color(0f, 0f, 0f, 0f);
            plateImage.raycastTarget = false;

            var host = root.transform;
            var title = Label(host, "Title", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.26f), TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Normal;
            title.resizeTextForBestFit = false;
            title.fontSize = 18;
            title.supportRichText = true;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            float barL = 0.02f;
            float barR = 0.98f;
            var health = Track(host, "Health", new Vector2(barL, 0.78f), new Vector2(barR, 0.98f), healthFill, bar);
            var healthNum = Caption(host, "HealthNum", new Vector2(barL + 0.08f, 0.78f), new Vector2(barR - 0.02f, 0.98f), TextAnchor.MiddleRight);
            var mana = Track(host, "Mana", new Vector2(barL, 0.56f), new Vector2(barR, 0.74f), manaFill, bar);
            var manaNum = Caption(host, "ManaNum", new Vector2(barL + 0.08f, 0.56f), new Vector2(barR - 0.02f, 0.74f), TextAnchor.MiddleRight);
            manaNum.color = new Color(0.73f, 0.9f, 0.98f, 1f);
            float pipY0 = 0.30f;
            float pipY1 = 0.52f;
            float pipW = 0.2f;
            float pip0 = barL;
            float pip1 = barL + 0.22f;
            float pip2 = barL + 0.44f;
            var stam = Track(host, "Stam", new Vector2(pip0, pipY0), new Vector2(pip0 + pipW, pipY1), stamFill, pip);
            var block = Track(host, "Block", new Vector2(pip1, pipY0), new Vector2(pip1 + pipW, pipY1), blockFill, pip);
            var dash = Track(host, "Dash", new Vector2(pip2, pipY0), new Vector2(pip2 + pipW, pipY1), dashFill, pip);
            var healthWord = Caption(host, "HealthWord", new Vector2(barL + 0.02f, 0.78f), new Vector2(barL + 0.16f, 0.98f), TextAnchor.MiddleLeft);
            healthWord.text = "HP";
            healthWord.resizeTextMaxSize = 12;
            var manaWord = Caption(host, "ManaWord", new Vector2(barL + 0.02f, 0.56f), new Vector2(barL + 0.16f, 0.74f), TextAnchor.MiddleLeft);
            manaWord.text = "MP";
            manaWord.resizeTextMaxSize = 12;
            var stamWord = Caption(host, "StamWord", new Vector2(pip0, pipY0), new Vector2(pip0 + pipW, pipY1), TextAnchor.MiddleCenter);
            stamWord.text = "ST";
            var blockNum = Caption(host, "BlockNum", new Vector2(pip1, pipY0), new Vector2(pip1 + pipW, pipY1), TextAnchor.MiddleCenter);
            blockNum.text = "BK";
            var dashNum = Caption(host, "DashNum", new Vector2(pip2, pipY0), new Vector2(pip2 + pipW, pipY1), TextAnchor.MiddleCenter);
            dashNum.text = "DA";

            var hud = root.AddComponent<LaneHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("health").objectReferenceValue = health;
            so.FindProperty("mana").objectReferenceValue = mana;
            so.FindProperty("stam").objectReferenceValue = stam;
            so.FindProperty("block").objectReferenceValue = block;
            so.FindProperty("dash").objectReferenceValue = dash;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("cards").objectReferenceValue = null;
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
            back.color = new Color(0.05f, 0.03f, 0.02f, 0.88f);
            back.raycastTarget = false;
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);
            var fill = fillObject.GetComponent<Image>();
            fill.sprite = fillSprite;
            fill.type = Image.Type.Simple;
            fill.preserveAspect = false;
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
            cover.type = Image.Type.Sliced;
            cover.fillCenter = true;
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
            face.type = Image.Type.Sliced;
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
            label.color = new Color(0xf6 / 255f, 0xe7 / 255f, 0xc1 / 255f, 1f);
            label.resizeTextForBestFit = false;
            label.fontSize = 14;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return button;
        }
    }
}
