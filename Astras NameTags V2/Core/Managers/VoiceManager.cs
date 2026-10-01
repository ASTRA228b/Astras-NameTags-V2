using Photon.Voice.PUN;
using UnityEngine;

namespace Astras_NameTags_V2.Core.Managers;

public static class VoiceManager
{
    private static PhotonVoiceView[] _views = [];
    private static float _nextScan;
    public static bool IsSpeaking(VRRig rig)
    {
        if (rig == null)
            return false;

        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + 1f;
            _views = GameObject.FindObjectsByType<PhotonVoiceView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        foreach (PhotonVoiceView view in _views)
        {
            if (view == null)
                continue;

            if (!view.IsSpeaking)
                continue;

            if (view.transform.IsChildOf(rig.transform))
                return true;
        }

        return false;
    }
}