using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleTv.Menu.EditorTools
{
    public static class WebGlPublish
    {
        const int PlateBytes = 282368;
        const int ClipBytes = 2145773;
        const int RestBytes = 3216639;
        const long TargetBytes = 20L * 1024L * 1024L;

        public static void Publish()
        {
            try
            {
                AssertSourceBytes();
                MenuSceneSetup.Build();
                GameMenuSetup.Build();
                ApplyPlayerSettings();
                var output = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs"));
                if (Directory.Exists(output))
                    Directory.Delete(output, true);
                Directory.CreateDirectory(output);

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Menu/Menu.unity", "Assets/Menu/GameMenu.unity" },
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("WebGL build failed: " + report.summary.result);

                var indexPath = Path.Combine(output, "index.html");
                InjectIntroBed(indexPath);
                AssertRelativePaths(indexPath);
                AssertPublishedClip(output);
                AssertPublishedRest(output);
                AssertIntroTrack(output);
                long bytes = WriteManifest(output);
                Debug.Log("[WebGlPublish] docs bytes " + bytes + (bytes <= TargetBytes ? " within 20 MB target" : " OVER 20 MB target"));
                EditorApplication.Exit(bytes >= 0 ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogError("[WebGlPublish] " + ex);
                EditorApplication.Exit(1);
            }
        }

        static void AssertSourceBytes()
        {
            var jpg = new FileInfo(Path.Combine(Application.dataPath, "Art", "Menu", "Vale-vs-Brute.jpg"));
            var mp4 = new FileInfo(Path.Combine(Application.dataPath, "StreamingAssets", "menu", "Vale-vs-Brute-push.mp4"));
            var rest = new FileInfo(Path.Combine(Application.dataPath, "StreamingAssets", "menu", "Vale-vs-Brute-rest.mp4"));
            if (jpg.Length != PlateBytes)
                throw new InvalidOperationException("Plate bytes " + jpg.Length);
            if (mp4.Length != ClipBytes)
                throw new InvalidOperationException("Clip bytes " + mp4.Length);
            if (rest.Length != RestBytes)
                throw new InvalidOperationException("Rest bytes " + rest.Length);
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Battle TV";
            PlayerSettings.companyName = "DeepSeekOracle";
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.initialMemorySize = 32;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
            foreach (var kind in new[] { LogType.Log, LogType.Warning, LogType.Error })
                PlayerSettings.SetStackTraceLogType(kind, StackTraceLogType.None);
            AssetDatabase.SaveAssets();
        }

        static void AssertRelativePaths(string indexPath)
        {
            var html = File.ReadAllText(indexPath);
            if (html.Contains("\"/Build") || html.Contains("'/Build") || html.Contains("\"/StreamingAssets") || html.Contains("'/StreamingAssets"))
                throw new InvalidOperationException("index.html uses a root-absolute /Build or /StreamingAssets path.");
        }

        const long IntroMp3Bytes = 4375885;

        static void InjectIntroBed(string indexPath)
        {
            var html = File.ReadAllText(indexPath);
            const string bed = @"
    <audio id=""intro-bed"" src=""StreamingAssets/menu/Apex_Vanguard.mp3"" loop autoplay preload=""auto""></audio>
    <script>
      window.battleTvStartIntro = function () {
        var bed = document.getElementById(""intro-bed"");
        if (!bed) return;
        bed.volume = 0.25;
        bed.loop = true;
        var play = bed.play();
        if (play && play.catch) play.catch(function () {});
      };
      window.battleTvStartIntro();
      document.addEventListener(""pointerdown"", window.battleTvStartIntro, true);
      document.addEventListener(""keydown"", window.battleTvStartIntro, true);
    </script>";
            const string body = "<body>";
            const string shown = "document.querySelector(\"#unity-loading-bar\").style.display = \"none\";";
            if (!html.Contains(body) || !html.Contains(shown))
                throw new InvalidOperationException("index.html has no place for the intro song.");
            if (!html.Contains("id=\"intro-bed\""))
                html = html.Replace(body, body + bed);
            if (!html.Contains("battleTvStartIntro) window.battleTvStartIntro"))
                html = html.Replace(shown, shown + "\n                if (window.battleTvStartIntro) window.battleTvStartIntro();");
            File.WriteAllText(indexPath, html);
        }

        static void AssertIntroTrack(string output)
        {
            var mp3 = new FileInfo(Path.Combine(output, "StreamingAssets", "menu", "Apex_Vanguard.mp3"));
            if (!mp3.Exists || mp3.Length != IntroMp3Bytes)
                throw new InvalidOperationException("Published intro mp3 bytes " + (mp3.Exists ? mp3.Length : 0));
        }

        static void AssertPublishedClip(string output)
        {
            var clip = new FileInfo(Path.Combine(output, "StreamingAssets", "menu", "Vale-vs-Brute-push.mp4"));
            if (!clip.Exists || clip.Length != ClipBytes)
                throw new InvalidOperationException("Published clip bytes " + (clip.Exists ? clip.Length : 0));
        }

        static void AssertPublishedRest(string output)
        {
            var rest = new FileInfo(Path.Combine(output, "StreamingAssets", "menu", "Vale-vs-Brute-rest.mp4"));
            if (!rest.Exists || rest.Length != RestBytes)
                throw new InvalidOperationException("Published rest bytes " + (rest.Exists ? rest.Length : 0));
        }

        static long WriteManifest(string output)
        {
            long bytes = Directory.GetFiles(output, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path).Length)
                .Sum();
            var sha = Environment.GetEnvironmentVariable("BATTLETV_GIT_SHA");
            if (string.IsNullOrWhiteSpace(sha))
                sha = "unpublished";
            var json = "{\n"
                + "  \"editor\": \"6000.5.9f1\",\n"
                + "  \"bytes\": " + bytes + ",\n"
                + "  \"gitSha\": \"" + sha + "\",\n"
                + "  \"plateBytes\": " + PlateBytes + ",\n"
                + "  \"clipBytes\": " + ClipBytes + ",\n"
                + "  \"restBytes\": " + RestBytes + ",\n"
                + "  \"targetBytes\": " + TargetBytes + ",\n"
                + "  \"withinTarget\": " + (bytes <= TargetBytes ? "true" : "false") + "\n"
                + "}\n";
            File.WriteAllText(Path.Combine(output, "build-manifest.json"), json);
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            return bytes + new FileInfo(Path.Combine(output, "build-manifest.json")).Length + 0;
        }
    }
}
