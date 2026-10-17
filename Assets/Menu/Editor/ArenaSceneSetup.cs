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
            so.FindProperty("barFrame").objectReferenceValue = HudSkin("bar-frame.png");
            so.FindProperty("pipFrame").objectReferenceValue = HudSkin("pip-frame.png");
            so.FindProperty("buttonSkin").objectReferenceValue = HudSkin("button.png");
            so.FindProperty("fillHealth").objectReferenceValue = HudSkin("fill-health.png");
            so.FindProperty("fillMana").objectReferenceValue = HudSkin("fill-mana.png");
            so.FindProperty("fillStam").objectReferenceValue = HudSkin("fill-stam.png");
            so.FindProperty("fillBlock").objectReferenceValue = HudSkin("fill-block.png");
            so.FindProperty("fillDash").objectReferenceValue = HudSkin("fill-dash.png");
            so.FindProperty("bruteRest").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-drag.png");
            so.FindProperty("bruteGrip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/brute-grip.png");
            AssignClip(so, "valeBash", Frames("vale-bash", 6));
            AssignClip(so, "valePunch", Frames("vale-punch", 6));
            AssignClip(so, "valeToss", Frames("vale-toss", 8));
            AssignClip(so, "valeLine", Frames("vale-line", 5));
            AssignClip(so, "valePlant", Frames("vale-plant", 4));
            AssignClip(so, "valeDeath", Frames("vale-death", 4));
            AssignClip(so, "valeWin", Frames("vale-win", 2));
            AssignClip(so, "bruteSweep", Frames("brute-sweep", 8));
            AssignClip(so, "brutePound", Frames("brute-pound", 2));
            AssignClip(so, "bruteWard", Frames("brute-ward", 2));
            AssignClip(so, "bruteClose", Frames("brute-close", 1));
            AssignClip(so, "bruteDeath", Frames("brute-death", 3));
            AssignClip(so, "bruteWin", Frames("brute-win", 2));
            var father = "Assets/Art/Duel/lightfather.png";
            Configure(father, true, 2048, true);
            so.FindProperty("lightfatherStill").objectReferenceValue = LoadSprite(father);
            AssignClip(so, "lfBolt", Cutout("lf-bolt", 4));
            AssignClip(so, "lfSeal", Cutout("lf-seal", 3));
            AssignClip(so, "lfProvenance", Cutout("lf-provenance", 3));
            AssignClip(so, "lfCover", Cutout("lf-cover", 4));
            AssignClip(so, "lfStandard", Cutout("lf-standard", 4));
            AssignClip(so, "lfSlip", Cutout("lf-slip", 2));
            AssignClip(so, "lfDeath", Cutout("lf-death", 1));
            AssignClip(so, "lfWin", Cutout("lf-win", 1));
            AssignClip(so, "valeWalk", Cutout("vale-walk", 8));
            AssignClip(so, "bruteWalk", Cutout("brute-walk", 8));
            AssignClip(so, "lfWalk", Cutout("lf-walk", 8));
            var kael = "Assets/Art/Duel/kael.png";
            Configure(kael, true, 2048, true);
            so.FindProperty("kaelStill").objectReferenceValue = LoadSprite(kael);
            AssignClip(so, "kaelShard", Cutout("kael-shard", 6));
            AssignClip(so, "kaelRend", Cutout("kael-rend", 3));
            AssignClip(so, "kaelWall", Cutout("kael-wall", 2));
            AssignClip(so, "kaelCharge", Cutout("kael-charge", 2));
            AssignClip(so, "kaelClose", Cutout("kael-close", 2));
            AssignClip(so, "kaelDeath", Cutout("kael-death", 1));
            AssignClip(so, "kaelWin", Cutout("kael-win", 1));
            AssignClip(so, "kaelWalk", Cutout("kael-walk", 8));
            var orin = "Assets/Art/Duel/orin.png";
            Configure(orin, true, 2048, true);
            so.FindProperty("orinStill").objectReferenceValue = LoadSprite(orin);
            AssignClip(so, "orinShard", Cutout("orin-shard", 3));
            AssignClip(so, "orinUnwrite", Cutout("orin-unwrite", 2));
            AssignClip(so, "orinFlask", Cutout("orin-flask", 2));
            AssignClip(so, "orinDrink", Cutout("orin-drink", 2));
            AssignClip(so, "orinSlip", Cutout("orin-slip", 2));
            AssignClip(so, "orinDeath", Cutout("orin-death", 1));
            AssignClip(so, "orinWin", Cutout("orin-win", 1));
            AssignClip(so, "orinWalk", Cutout("orin-walk", 8));
            var nia = "Assets/Art/Duel/nia.png";
            Configure(nia, true, 2048, true);
            var niaIdle = LoadSprite(nia);
            so.FindProperty("niaStill").objectReferenceValue = niaIdle;
            // Idle frames reuse the still. A second copy of the same plate was pushing the WebGL data over GitHub's 100 MB limit.
            var niaArrow = Cutout("nia-arrow", 3);
            niaArrow[0] = niaIdle;
            niaArrow[2] = niaIdle;
            AssignClip(so, "niaArrow", niaArrow);
            var niaPin = Cutout("nia-pin", 2);
            niaPin[0] = niaIdle;
            AssignClip(so, "niaPin", niaPin);
            var niaDash = Cutout("nia-dash", 2);
            niaDash[1] = niaIdle;
            AssignClip(so, "niaDash", niaDash);
            var niaRain = Cutout("nia-rain", 2);
            niaRain[0] = niaIdle;
            AssignClip(so, "niaRain", niaRain);
            var niaSlip = Cutout("nia-slip", 2);
            niaSlip[1] = niaIdle;
            AssignClip(so, "niaSlip", niaSlip);
            AssignClip(so, "niaDeath", Cutout("nia-death", 1));
            AssignClip(so, "niaWin", Cutout("nia-win", 1));
            AssignClip(so, "niaWalk", Cutout("nia-walk", 8));
            var lyra = "Assets/Art/Duel/lyra.png";
            Configure(lyra, true, 2048, true);
            var lyraIdle = LoadSprite(lyra);
            so.FindProperty("lyraStill").objectReferenceValue = lyraIdle;
            // Idle frames reuse the still so the WebGL data file does not store the same plate again.
            var lyraNote = Cutout("lyra-note", 3);
            lyraNote[0] = lyraIdle;
            lyraNote[2] = lyraIdle;
            AssignClip(so, "lyraNote", lyraNote);
            var lyraHymn = Cutout("lyra-hymn", 2);
            lyraHymn[0] = lyraIdle;
            AssignClip(so, "lyraHymn", lyraHymn);
            var lyraChorus = Cutout("lyra-chorus", 2);
            lyraChorus[0] = lyraIdle;
            AssignClip(so, "lyraChorus", lyraChorus);
            var lyraBloom = Cutout("lyra-bloom", 2);
            lyraBloom[0] = lyraIdle;
            AssignClip(so, "lyraBloom", lyraBloom);
            var lyraSlip = Cutout("lyra-slip", 2);
            lyraSlip[1] = lyraIdle;
            AssignClip(so, "lyraSlip", lyraSlip);
            AssignClip(so, "lyraDeath", Cutout("lyra-death", 1));
            AssignClip(so, "lyraWin", Cutout("lyra-win", 1));
            AssignClip(so, "lyraWalk", Cutout("lyra-walk", 8));
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Duel/vale-door/glow-white.png");
            if (glow == null)
                throw new FileNotFoundException("Sigil plate did not import.", "Assets/Art/Duel/vale-door/glow-white.png");
            so.FindProperty("sigilGlow").objectReferenceValue = glow;
            so.FindProperty("sigilRingBack").objectReferenceValue = SigilMat("Assets/Fight/SigilRingBack.mat", 1f, 1f);
            so.FindProperty("sigilRingFront").objectReferenceValue = SigilMat("Assets/Fight/SigilRingFront.mat", 1f, -1f);
            so.FindProperty("sigilBolt").objectReferenceValue = SigilMat("Assets/Fight/SigilBolt.mat", 0f, 0f);
            so.FindProperty("sigilBurst").objectReferenceValue = SigilMat("Assets/Fight/SigilBurst.mat", 3f, 0f);
            so.FindProperty("sigilMend").objectReferenceValue = SigilMat("Assets/Fight/SigilMend.mat", 2f, 0f);
            so.FindProperty("sigilEmber").objectReferenceValue = SigilMat("Assets/Fight/SigilEmber.mat", 5f, 0f);
            so.FindProperty("sigilLance").objectReferenceValue = SigilMat("Assets/Fight/SigilLance.mat", 4f, 0f);
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

        static Sprite[] Frames(string folder, int count)
        {
            return LoadFrames(folder, count, false);
        }

        static Sprite[] Cutout(string folder, int count)
        {
            return LoadFrames(folder, count, true);
        }

        static Sprite[] LoadFrames(string folder, int count, bool cutout)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                var path = "Assets/Art/Duel/" + folder + "/f" + i.ToString("00") + ".png";
                if (cutout)
                    Configure(path, true, 2048, true);
                frames[i] = LoadSprite(path);
                if (frames[i] == null)
                    throw new FileNotFoundException("Arena clip frame did not import.", path);
            }
            return frames;
        }

        static void AssignClip(SerializedObject so, string property, Sprite[] frames)
        {
            var prop = so.FindProperty(property);
            prop.arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        static Sprite HudSkin(string file)
        {
            var path = "Assets/Art/Duel/ui/" + file;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException("HUD skin did not import.", path);
            return sprite;
        }

        public static void ImportFatherStill()
        {
            Configure("Assets/Art/Duel/lightfather.png", true, 2048, true);
        }

        public static void ImportKael()
        {
            Configure("Assets/Art/Duel/kael.png", true, 2048, true);
        }

        public static void ImportOrin()
        {
            Configure("Assets/Art/Duel/orin.png", true, 2048, true);
        }

        public static void ImportNia()
        {
            Configure("Assets/Art/Duel/nia.png", true, 2048, true);
        }

        public static void ImportLyra()
        {
            Configure("Assets/Art/Duel/lyra.png", true, 2048, true);
        }

        public static Sprite LoadPlate(string path)
        {
            return LoadSprite(path);
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] is Sprite found)
                    return found;
            }
            return null;
        }

        static Material SigilMat(string path, float kind, float half)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Fight/ArenaSigil.shader");
            if (shader == null)
                throw new FileNotFoundException("Arena sigil shader is missing.", "Assets/Fight/ArenaSigil.shader");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
                mat.shader = shader;
            mat.SetFloat("_Kind", kind);
            mat.SetFloat("_Half", half);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static void Configure(string path, bool alpha, int max, bool fullPlate = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Arena art is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = fullPlate ? SpriteImportMode.Multiple : SpriteImportMode.Single;
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
            if (fullPlate)
            {
                importer.GetSourceTextureWidthAndHeight(out var w, out var h);
                importer.spritesheet = new[]
                {
                    new SpriteMetaData
                    {
                        name = Path.GetFileNameWithoutExtension(path),
                        rect = new Rect(0f, 0f, w, h),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f)
                    }
                };
            }
            importer.SaveAndReimport();
        }
    }
}
