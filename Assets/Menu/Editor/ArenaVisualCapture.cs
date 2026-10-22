using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BattleTv.Menu.EditorTools
{
    [InitializeOnLoad]
    public static class ArenaVisualCapture
    {
        const string ScenePath = "Assets/Fight/Arena.unity";
        const string RelOut = "Logs/arena-hint-1280x720.png";
        const string ArmedKey = "LYGO.ArenaVisualCapture.Armed";

        static double _started;
        static double _foundAt;
        static bool _armed;
        static bool _captured;

        static ArenaVisualCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;
            if (!SessionState.GetBool(ArmedKey, false))
                return;
            Debug.Log("[ArenaVisualCapture] EnteredPlayMode; re-arming armed capture.");
            _started = Time.realtimeSinceStartup;
            _foundAt = 0;
            _armed = false;
            _captured = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Capture()
        {
            Debug.Log("[ArenaVisualCapture] Begin graphical capture.");
            SessionState.SetBool(ArmedKey, true);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("[ArenaVisualCapture] Arena scene did not open.");
                EditorApplication.Exit(2);
                return;
            }
            Screen.SetResolution(1280, 720, false);
            _started = Time.realtimeSinceStartup;
            _foundAt = 0;
            _armed = false;
            _captured = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            var elapsed = Time.realtimeSinceStartup - _started;
            if (!_armed)
            {
                var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var found = false;
                foreach (var text in texts)
                {
                    if (text != null && text.text == BattleTv.Fight.ArenaView.OnboardingLine)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (elapsed > 12f)
                    {
                        Debug.LogError("[ArenaVisualCapture] onboarding Text not found in 12s.");
                        EditorApplication.Exit(3);
                    }
                    return;
                }
                _armed = true;
                _foundAt = Time.realtimeSinceStartup;
                Debug.Log("[ArenaVisualCapture] Onboarding Text found; waiting for render.");
                return;
            }
            if (_captured || Time.realtimeSinceStartup - _foundAt < 0.75f)
                return;
            _captured = true;
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RelOut));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var ok = CaptureRenderTexture(path, 1280, 720);
            Debug.Log("[ArenaVisualCapture] render texture " + (ok ? "wrote " + path + " " + new FileInfo(path).Length + " bytes" : "failed"));
            EditorApplication.update -= Tick;
            SessionState.SetBool(ArmedKey, false);
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.Refresh();
                EditorApplication.Exit(ok ? 0 : 4);
            };
        }

        static bool CaptureRenderTexture(string path, int width, int height)
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (canvas == null || camera == null)
            {
                Debug.LogError("[ArenaVisualCapture] Canvas or Camera missing.");
                return false;
            }
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldTarget = camera.targetTexture;
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                rt.Create();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return File.Exists(path) && new FileInfo(path).Length > 1000;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ArenaVisualCapture] RenderTexture capture failed: " + ex);
                return false;
            }
            finally
            {
                camera.targetTexture = oldTarget;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                RenderTexture.active = null;
                if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
        }
    }
}
