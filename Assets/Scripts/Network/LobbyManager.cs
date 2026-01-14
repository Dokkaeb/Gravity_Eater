using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI연결")]
    [SerializeField] TMP_InputField _nickNameInput;
    [SerializeField] Button _joinButton;
    [SerializeField] TextMeshProUGUI _statusText;

    string _gameVersion = "1";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void OnClickConnect()
    {
        string name = _nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        PhotonNetwork.LocalPlayer.NickName = name;
        PhotonNetwork.GameVersion = _gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        _joinButton.interactable = false;
        _statusText.text = "server connecting...";
    }

    public override void OnConnectedToMaster()
    {
        _statusText.text = "room enter";
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 방이 없으면 새로 생성 (최대 20명)
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        _statusText.text = "move game scene";
        // 방장이면 씬을 전환 (AutomaticallySyncScene에 의해 다 같이 이동됨)
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("InGame");
        }
    }
}
