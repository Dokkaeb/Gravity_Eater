using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI연결")]
    [SerializeField] InputField _nickNameInput;
    [SerializeField] Button _joinButton;
    [SerializeField] Text _statusText;

    string _gameVersion = "1";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady) //이미 연결되있나 확인
        {
            OnConnectedToMaster();
        }
        else
        {
            _statusText.text = "서버연결대기중 이름 입력후 start버튼 눌러주세요";
            _joinButton.interactable = true;
        }
    }

    public void OnClickConnect()
    {
        string name = _nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        PhotonNetwork.LocalPlayer.NickName = name;
        _joinButton.interactable = false;

        if (!PhotonNetwork.IsConnected) //서버연결 안된경우(처음실행)
        {
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            _statusText.text = "서버 접속중";
        }
        else if (PhotonNetwork.IsConnectedAndReady) //서버연결되있는데 방에만 안들어간 경우(인게임에서 복귀했을때)
        {
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.GenerateNewUserID();
                Debug.Log("새로운 판을 위한 ID 발급 완료");
            }

            _statusText.text = "빈방 찾는중";
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        _statusText.text = "서버 연결 완료 start 버튼 누르세요.";
        _joinButton.interactable = true;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 방이 없으면 새로 생성 (최대 20명)
        _statusText.text = "방이 없어 새로 생성합니다";
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        _statusText.text = "게임 입장 중";
        // 방장이면 씬을 전환 (AutomaticallySyncScene에 의해 다 같이 이동됨)
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("InGame");
        }
    }

    // 연결이 끊겼을 때 콜백
    public override void OnDisconnected(DisconnectCause cause)
    {
        _statusText.text = $"연결 끊김: {cause}";
        _joinButton.interactable = true;
    }
}
