using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace QAPI;

public static class Resources
{
    #region Variables
    internal static Dictionary<string, Il2CppAssetBundle> BundleHash = new();
    #endregion

    #region Bundles

    /// <summary>
    /// Loads in a bundle. Returns null on error.
    /// </summary>
    public static Il2CppAssetBundle? LoadBundle(string bundleName)
    {
        BundleHash ??= new Dictionary<string, Il2CppAssetBundle>();

        var assembly = System.Reflection.Assembly.GetCallingAssembly();

        bundleName = $"{assembly.GetName().Name}.{bundleName}";

        try
        {
            Il2CppAssetBundle? bundle = null;

            Log.LogOutput(
                $"Loading '{bundleName}' from assembly '{assembly.FullName}'",
                Log.ELevel.Debug
            );

            foreach (string fileName in assembly.GetManifestResourceNames())
                Log.LogOutput($"Assembly resources: {fileName}", Log.ELevel.Debug);

            MemoryStream memoryStream;

            using (Stream stream = assembly.GetManifestResourceStream(bundleName))
            {
                memoryStream = new MemoryStream((int)stream.Length);
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                memoryStream.Write(buffer, 0, buffer.Length);
            }

            bundle = Il2CppAssetBundleManager.LoadFromMemory(memoryStream.ToArray());

            if (bundle == null)
            {
                Log.LogOutput($"Unable to load '{bundleName}'.", Log.ELevel.Error);
                return null;
            }
            else
            {
                BundleHash.Add(bundleName, bundle);
                Log.LogOutput($"Registered bundle '{bundleName}'", Log.ELevel.Message);
                return bundle;
            }
        }
        catch (Exception e)
        {
            Log.LogOutput(e, Log.ELevel.Error);
            return null;
        }
    }

    /// <summary>
    /// Loads in a bundle and makes it available to other mods. Returns empty on error.
    /// </summary>
    public static string RegisterBundle(string bundleName)
    {
        BundleHash ??= new Dictionary<string, Il2CppAssetBundle>();

        var assembly = System.Reflection.Assembly.GetCallingAssembly();

        bundleName = $"{assembly.GetName().Name}.{bundleName}";

        if (BundleHash.ContainsKey(bundleName))
        {
            Log.LogOutput($"Bundle '{bundleName}' already loaded", Log.ELevel.Warning);
            return String.Empty;
        }

        try
        {
            Il2CppAssetBundle? bundle = null;

            Log.LogOutput(
                $"Loading '{bundleName}' from assembly '{assembly.FullName}'",
                Log.ELevel.Debug
            );

            foreach (string fileName in assembly.GetManifestResourceNames())
                Log.LogOutput($"Assembly resources: {fileName}", Log.ELevel.Debug);

            MemoryStream memoryStream;

            using (Stream stream = assembly.GetManifestResourceStream(bundleName))
            {
                memoryStream = new MemoryStream((int)stream.Length);
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                memoryStream.Write(buffer, 0, buffer.Length);
            }

            bundle = Il2CppAssetBundleManager.LoadFromMemory(memoryStream.ToArray());

            if (bundle == null)
            {
                Log.LogOutput($"Unable to load '{bundleName}'.", Log.ELevel.Error);
            }
            else
            {
                BundleHash.Add(bundleName, bundle);
                Log.LogOutput($"Registered bundle '{bundleName}'", Log.ELevel.Message);
            }
        }
        catch (Exception e)
        {
            Log.LogOutput(e, Log.ELevel.Error);
            return String.Empty;
        }

        return bundleName;
    }

    public static Il2CppAssetBundle? GetBundle(string key)
    {
        if (String.IsNullOrEmpty(key))
        {
            Log.LogOutput($"GetBundle: key is null or empty", Log.ELevel.Warning);
            return null;
        }

        if (!BundleHash.ContainsKey(key))
        {
            Log.LogOutput($"GetBundle: '{key}' not found", Log.ELevel.Warning);
            return null;
        }

        return BundleHash[key];
    }
    #endregion
}
