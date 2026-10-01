using BepInEx;
using UnityEngine;
using Astras_NameTags_V2.Stuff;
using Astras_NameTags_V2.Core.Managers;

namespace Astras_NameTags_V2;

[BepInPlugin(Constantss.GUID, Constantss.Name, Constantss.Version)]
public class Plugin : BaseUnityPlugin
{
    void Awake()
    {
        AssetManager.Load();
        GameObject Plugin = new GameObject(Constantss.ObjectName);
        Plugin.AddComponent<NameTagManager>();
        DontDestroyOnLoad(Plugin);
    }
} // Astras_NameTags_V2
