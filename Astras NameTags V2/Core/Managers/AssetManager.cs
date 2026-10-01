using UnityEngine;
using System.Reflection;
using Astras_NameTags_V2.Stuff;

namespace Astras_NameTags_V2.Core.Managers;

public static class AssetManager
{
    public static Texture2D? SpeakerIcon { get; private set; }

    public static void Load()
    {
        if (SpeakerIcon != null)
            return;

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("nametags.png", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
            {
                Debug.LogError($"[{Constantss.GUID}] Could not find nametags.png");
                foreach (string resource in assembly.GetManifestResourceNames())
                    Debug.Log($"[{Constantss.GUID}] Found resource: {resource}");

                return;
            }
            Debug.Log($"[{Constantss.GUID}] Loading resource: {resourceName}");
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.LogError($"[{Constantss.GUID}] Resource stream was null");
                return;
            }
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data))
            {
                Debug.LogError($"[{Constantss.GUID}] LoadImage failed");
                return;
            }
            texture.name = "SpeakerIcon";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            SpeakerIcon = texture;
            Debug.Log($"[{Constantss.GUID}] Speaker Loaded " + $"({texture.width}x{texture.height})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{Constantss.GUID}] Speaker failed: {e}");
        }
    }
}