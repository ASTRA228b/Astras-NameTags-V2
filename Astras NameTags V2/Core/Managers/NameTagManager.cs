using Astras_NameTags_V2.Stuff;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astras_NameTags_V2.Core.Managers;

public class NameTagManager : MonoBehaviour
{
    private const float TagHeight = 0.80f;
    private const float TagScale = 1f;
    private const float PlayerScanInterval = 1f;
    private const float TextUpdateInterval = 0.25f;
    private readonly Dictionary<VRRig, PlayerTag> _tags = new();
    private float _nextPlayerScan;
    private float _nextTextUpdate;
    private Camera? _camera;
    private static PhotonVoiceView[] _voiceViews = [];
    private static float _nextVoiceScan;
    private static readonly FieldInfo? FpsField = typeof(VRRig).GetField("fps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private void Update()
    {
        if (!PhotonNetwork.InRoom)
        {
            if (_tags.Count > 0) DestroyAllTags();
            return;
        }
        if (_camera == null) _camera = Camera.main;
        if (Time.unscaledTime >= _nextPlayerScan)
        {
            _nextPlayerScan = Time.unscaledTime + PlayerScanInterval;
            RefreshPlayers();
        }
        bool updateText = Time.unscaledTime >= _nextTextUpdate;
        if (updateText)
            _nextTextUpdate = Time.unscaledTime + TextUpdateInterval;

        UpdateTags(updateText);
    }

    private void RefreshPlayers()
    {
        GorillaGameManager? game = GorillaGameManager.instance;
        if (game == null) return;
        HashSet<VRRig> activeRigs = new();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == null || player == PhotonNetwork.LocalPlayer) continue;

            try
            {
                VRRig rig = game.FindPlayerVRRig((NetPlayer)player);
                if (rig == null) continue;

                activeRigs.Add(rig);

                if (!_tags.ContainsKey(rig))
                    _tags.Add(rig, CreateTag(rig));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{Constantss.GUID}] Failed: {e.Message}");
            }
        }

        List<VRRig> remove = new();

        foreach (KeyValuePair<VRRig, PlayerTag> pair in _tags)
        {
            if (pair.Key == null)
                continue;

            if (!activeRigs.Contains(pair.Key))
                remove.Add(pair.Key);
        }

        foreach (VRRig rig in remove)
            RemoveTag(rig);
    }

    private PlayerTag CreateTag(VRRig rig)
    {
        GameObject root = new("Astras NameTagss");
        root.transform.SetParent(rig.transform, false);
        root.transform.localPosition = Vector3.up * TagHeight;
        root.transform.localScale = Vector3.one * TagScale;
        GameObject textObject = new("Text");
        textObject.transform.SetParent(root.transform, false);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 4f;
        text.fontStyle = FontStyles.Bold;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.15f;
        text.rectTransform.sizeDelta = new Vector2(8f, 3f);
        GameObject canvasObject = new("SpeakerCanvas");
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(140f, 80f);
        canvasRect.localScale = Vector3.one * 0.015f;
        canvasRect.localPosition = new Vector3(0f, 0.36f, -0.02f);
        GameObject iconObject = new("SpeakerIcon");
        iconObject.transform.SetParent(canvasObject.transform, false);
        RawImage icon = iconObject.AddComponent<RawImage>();
        icon.texture = AssetManager.SpeakerIcon;
        icon.raycastTarget = false;
        icon.enabled = false;
        icon.rectTransform.sizeDelta = new Vector2(35f, 35f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 19f);
        Debug.Log($"[{Constantss.GUID}] Speaker texture: {(icon.texture != null ? "LOADED" : "NULL")}");
        PhotonVoiceView? voiceView = FindVoiceView(rig);
        PlayerTag tag = new(root, text, icon, voiceView);
        UpdateText(rig, tag);
        return tag;
    }

    private static PhotonVoiceView? FindVoiceView(VRRig rig)
    {
        if (rig == null || rig.Creator == null)
            return null;

        try
        {
            PhotonVoiceView? view = rig.GetComponent<PhotonVoiceView>();

            if (view != null)
                return view;

            view = rig.GetComponentInChildren<PhotonVoiceView>(true);

            if (view != null)
                return view;

            if (Time.unscaledTime >= _nextVoiceScan)
            {
                _nextVoiceScan = Time.unscaledTime + 1f;
                _voiceViews = FindObjectsByType<PhotonVoiceView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            foreach (PhotonVoiceView candidate in _voiceViews)
            {
                if (candidate == null)
                    continue;

                PhotonView? photonView = candidate.GetComponent<PhotonView>();

                if (photonView == null)
                    photonView = candidate.GetComponentInParent<PhotonView>();

                if (photonView != null && photonView.OwnerActorNr == rig.Creator.ActorNumber)
                    return candidate;

                if (candidate.transform.IsChildOf(rig.transform))
                    return candidate;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{Constantss.GUID}] Voice view search failed: {e.Message}");
        }

        return null;
    }

    private void UpdateTags(bool updateText)
    {
        List<VRRig> remove = new();

        foreach (KeyValuePair<VRRig, PlayerTag> pair in _tags)
        {
            VRRig rig = pair.Key;
            PlayerTag tag = pair.Value;

            if (rig == null)
                continue;

            if (tag.Object == null)
            {
                remove.Add(rig);
                continue;
            }

            tag.Object.transform.localPosition = Vector3.up * TagHeight;
            FaceCamera(tag.Object.transform);
            UpdateSpeaker(rig, tag);

            if (updateText)
                UpdateText(rig, tag);
        }

        foreach (VRRig rig in remove)
            RemoveTag(rig);
    }

    private static void UpdateSpeaker(VRRig rig, PlayerTag tag)
    {
        if (tag.SpeakerIcon == null)
            return;

        if (tag.SpeakerIcon.texture == null && AssetManager.SpeakerIcon != null)
            tag.SpeakerIcon.texture = AssetManager.SpeakerIcon;

        if (tag.VoiceView == null)
            tag.VoiceView = FindVoiceView(rig);

        try
        {
            bool speaking = tag.VoiceView != null && tag.VoiceView.IsSpeaking;
            tag.SpeakerIcon.enabled = speaking && tag.SpeakerIcon.texture != null;
        }
        catch
        {
            tag.SpeakerIcon.enabled = false;
        }
    }

    private void FaceCamera(Transform tag)
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        Vector3 direction = tag.position - _camera.transform.position;

        if (direction.sqrMagnitude > 0.001f)
            tag.rotation = Quaternion.LookRotation(direction);
    }

    private static void UpdateText(VRRig rig, PlayerTag tag)
    {
        string name = GetPlayerName(rig);
        string playerColor = GetPlayerColor(rig);
        int fps = GetFPS(rig);
        string fpsValue = fps >= 0 ? fps.ToString() : "N/A";
        string fpsColor = fps >= 0 ? GetFPSColor(fps) : "BFBFBF";

        tag.Text.text = $"<color=#{playerColor}>{name}</color>\n" + $"<color=#FFFFFF>FPS: </color><color=#{fpsColor}>{fpsValue}</color>";
    }

    private static string GetFPSColor(int fps)
    {
        if (fps >= 72) return "55FF55";
        if (fps >= 55) return "FFD84A";
        return "FF5555";
    }

    private static string GetPlayerName(VRRig rig)
    {
        try
        {
            string name = rig.playerText1?.text ?? "";

            if (string.IsNullOrWhiteSpace(name))
                name = rig.Creator?.NickName ?? "";

            if (string.IsNullOrWhiteSpace(name))
                name = "PLAYER";

            return name.Replace("<", "＜").Replace(">", "＞");
        }
        catch
        {
            return "PLAYER";
        }
    }

    private static string GetPlayerColor(VRRig rig)
    {
        try
        {
            return ColorUtility.ToHtmlStringRGB(rig.playerColor);
        }
        catch
        {
            return "FFFFFF";
        }
    }

    private static int GetFPS(VRRig rig)
    {
        if (rig == null || FpsField == null) return -1;

        try
        {
            object? value = FpsField.GetValue(rig);
            return value == null ? -1 : Convert.ToInt32(value);
        }
        catch
        {
            return -1;
        }
    }

    private void RemoveTag(VRRig rig)
    {
        if (rig == null || !_tags.TryGetValue(rig, out PlayerTag? tag)) return;

        if (tag.Object != null)
            Destroy(tag.Object);

        _tags.Remove(rig);
    }

    private void DestroyAllTags()
    {
        foreach (PlayerTag tag in _tags.Values)
        {
            if (tag.Object != null)
                Destroy(tag.Object);
        }

        _tags.Clear();
    }

    private void OnDestroy()
    {
        DestroyAllTags();
    }
}