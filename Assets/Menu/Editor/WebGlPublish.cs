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
        const long TargetBytes = 20L * 1024L * 1024L;

        public static void Publish()
        {
            try
            {
                AssertSourceBytes();
                MenuSceneSetup.Build();
                ApplyPlayerSettings();
                var output = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs"));
                if (Directory.Exists(output))
                    Directory.Delete(output, true);
                Directory.CreateDirectory(output);

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Menu/Menu.unity" },
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("WebGL build failed: " + report.summary.result);

                AssertRelativePaths(Path.Combine(output, "index.html"));
                AssertPublishedClip(output);
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
            if (jpg.Length != PlateBytes)
                throw new InvalidOperationException("Plate bytes " + jpg.Length);
            if (mp4.Length != ClipBytes)
                throw new InvalidOperationException("Clip bytes " + mp4.Length);
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

        static void AssertPublishedClip(string output)
        {
            var clip = new FileInfo(Path.Combine(output, "StreamingAssets", "menu", "Vale-vs-Brute-push.mp4"));
            if (!clip.Exists || clip.Length != ClipBytes)
                throw new InvalidOperationException("Published clip bytes " + (clip.Exists ? clip.Length : 0));
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
                + "  \"targetBytes\": " + TargetBytes + ",\n"
                + "  \"withinTarget\": " + (bytes <= TargetBytes ? "true" : "false") + "\n"
                + "}\n";
            File.WriteAllText(Path.Combine(output, "build-manifest.json"), json);
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            return bytes + new FileInfo(Path.Combine(output, "build-manifest.json")).Length + 0;
        }
    }
}
