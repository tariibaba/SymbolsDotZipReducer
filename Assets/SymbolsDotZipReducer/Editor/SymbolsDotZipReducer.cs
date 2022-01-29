#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ionic.Zip;

[InitializeOnLoad]
public class SymbolsDotZipReducer : IPostprocessBuildWithReport, IPreprocessBuildWithReport
{
    static SymbolsDotZipReducer()
    {

    }

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {

    }

    public void OnPostprocessBuild(BuildReport report)
    {
        var buildingForAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
        var createSymbolsZip = EditorUserBuildSettings.androidCreateSymbolsZip;
        if (!(buildingForAndroid && createSymbolsZip)) return;
        var symbolsZipPath = GetSymbolsDotZipPath();
        var progressId = Progress.Start("Reducing symbols.zip");
        ReduceZipFile(symbolsZipPath).ContinueWith((task) =>
        {
            Progress.Remove(progressId);
        });
    }

    private static string GetSymbolsDotZipPath()
    {
        var method = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod(
            "GetBuildPlayerOptionsInternal",
            BindingFlags.NonPublic | BindingFlags.Static);
        var options = (BuildPlayerOptions)method.Invoke(
            null,
            new object[] { false, new BuildPlayerOptions() }
        );
        var fileName = Path.GetFileNameWithoutExtension(options.locationPathName);
        var dirName = Path.GetDirectoryName(options.locationPathName);
        var version = PlayerSettings.bundleVersion;
        var versionCode = PlayerSettings.Android.bundleVersionCode;
        var symbolsZipName = string.Format("{0}-{1}-v{2}.symbols.zip", fileName, version, versionCode);
        return Path.Combine(dirName, symbolsZipName);
    }

    private async Task ReduceZipFile(string path)
    {
        await Task.Run(() =>
        {
            var dirPath = Path.GetDirectoryName(path);
            var tempFolderPath = Path.Combine(dirPath, "temp");
            var file = new ZipFile(path);
            file.ExtractAll(tempFolderPath);
            file.Dispose();
            File.Delete(path);
            var recompressed = new ZipFile(path);
            recompressed.AddDirectory(tempFolderPath);
            recompressed.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
            recompressed.Save();
            recompressed.Dispose();
            Directory.Delete(tempFolderPath, true);
        });
    }
}
#endif
