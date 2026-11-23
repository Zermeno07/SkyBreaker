using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Genera la versión Beta de Sky Breaker con un clic desde el menú "Sky Breaker" de Unity.
// Crea la escena si no existe, configura el nombre y la versión del juego, y deja el
// ejecutable con todos sus recursos en la carpeta Builds del proyecto.
public static class BuildBeta
{
    const string ScenePath = "Assets/Scenes/Juego.unity";
    const string Version = "0.9.0-beta";

    [MenuItem("Sky Breaker/Generar versión Beta (Windows)")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/SkyBreaker_Beta_Windows/SkyBreaker.exe");
    }

    [MenuItem("Sky Breaker/Generar versión Beta (Android)")]
    public static void BuildAndroid()
    {
        Build(BuildTarget.Android, "Builds/SkyBreaker_Beta_Android/SkyBreaker_Beta.apk");
    }

    // El juego se crea por código al iniciar, así que basta con una escena vacía
    static void EnsureScene()
    {
        if (File.Exists(ScenePath)) return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }

    static void Build(BuildTarget target, string path)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureScene();

        PlayerSettings.companyName = "IDGS10D11";
        PlayerSettings.productName = "Sky Breaker";
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.idgs10d11.skybreaker");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0, como pide el GDD

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            string msg = "Versión Beta generada en:\n" + Path.GetFullPath(path) +
                         "\n\nTamaño: " + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB" +
                         "\nTiempo: " + summary.totalTime.TotalSeconds.ToString("0") + " s";
            Debug.Log(msg);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Sky Breaker", msg, "Aceptar");
                EditorUtility.RevealInFinder(path);
            }
        }
        else
        {
            string msg = "No se pudo generar la versión Beta (" + summary.result + "). Revisa la consola. " +
                         "Si es Android, instala el módulo Android Build Support desde Unity Hub.";
            Debug.LogError(msg);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Sky Breaker", msg, "Aceptar");
        }
    }
}
