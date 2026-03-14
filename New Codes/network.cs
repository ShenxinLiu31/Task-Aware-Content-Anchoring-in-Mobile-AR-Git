using Photon.Realtime;
using Photon.Voice.Unity;
using UnityEngine;

public class VoiceAutoJoin : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks
{
    public UnityVoiceClient voice;               // 拖到 Inspector
    public string roomName = "HL2MicRoom";       // 两端一致

    void Awake()
    {
        if (!voice) voice = FindObjectOfType<UnityVoiceClient>();
        voice.Client.AddCallbackTarget(this);
        voice.ConnectUsingSettings();            // 用 Inspector 的 AppId/Region 连接
    }
    void OnDestroy() { if (voice) voice.Client.RemoveCallbackTarget(this); }

    public void OnConnectedToMaster()
    {
        voice.Client.OpJoinOrCreateRoom(new EnterRoomParams { RoomName = roomName });
        Debug.Log("[VOICE] Connected. Joining room: " + roomName);
    }
    public void OnJoinedRoom() => Debug.Log("[VOICE] Joined room: " + roomName);

    // 其余接口留空/打印即可
    public void OnConnected() { }
    public void OnRegionListReceived(RegionHandler rh) { }
    public void OnDisconnected(DisconnectCause cause) { Debug.LogWarning("[VOICE] Disconnected: " + cause); }
    public void OnCustomAuthenticationResponse(System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string msg) { Debug.LogError("[VOICE] Auth failed: " + msg); }
    public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> _) { }
    public void OnCreatedRoom() { }
    public void OnCreateRoomFailed(short code, string msg) { Debug.LogError("[VOICE] Create room failed: " + msg); }
    public void OnJoinRoomFailed(short code, string msg) { Debug.LogError("[VOICE] Join room failed: " + msg); }
    public void OnJoinRandomFailed(short code, string msg) { Debug.LogError("[VOICE] Join random failed: " + msg); }
    public void OnLeftRoom() { }
}