using TMPro;
using Photon.Voice.PUN;
using UnityEngine;
using UnityEngine.UI;

namespace Astras_NameTags_V2.Core.Managers;

public class PlayerTag
{
    public GameObject Object;
    public TextMeshPro Text;
    public RawImage SpeakerIcon;
    public PhotonVoiceView? VoiceView;

    public PlayerTag(GameObject obj, TextMeshPro text, RawImage speakerIcon, PhotonVoiceView? voiceView)
    {
        Object = obj;
        Text = text;
        SpeakerIcon = speakerIcon;
        VoiceView = voiceView;
    }
}