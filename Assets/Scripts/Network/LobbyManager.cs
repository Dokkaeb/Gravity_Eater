using UnityEngine;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }
    string _gameVersion = "1";

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    private async void Start()
    {
        LobbyUIManager.Instance.UpdateStatus("시스템 초기화 중...");

        //파이어 베이스 초기화 대기
        while (FirebaseManager.Instance == null) await Task.Yield();
        await FirebaseManager.Instance.WaitForInitialization;

        LobbyUIManager.Instance.InitLeaderboard();

        //서버 연결상태 확인
        if (PhotonNetwork.IsConnectedAndReady) //이미 연결되있나 확인
        {
            OnConnectedToMaster();
        }
        else
        {
            LobbyUIManager.Instance.UpdateStatus("이름 입력 후 Start를 누르세요.");
            LobbyUIManager.Instance.SetJoinButtonActive(true);
        }
    }

    public void Connect(string nickName)
    {        
        PhotonNetwork.LocalPlayer.NickName = nickName;

        if (!PhotonNetwork.IsConnected) //서버연결 안된경우(처음실행)
        {
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            LobbyUIManager.Instance.UpdateStatus("서버 접속 중...");
        }
        else
        {
            JoinGame();
        }
    }

    public void JoinGame()
    {
        if (FirebaseManager.Instance != null)
            FirebaseManager.Instance.GenerateNewUserID();

        LobbyUIManager.Instance.UpdateStatus("빈 방 찾는 중...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnConnectedToMaster()
    {
        LobbyUIManager.Instance.UpdateStatus("서버 연결 완료! start 누르세요");
        LobbyUIManager.Instance.SetJoinButtonActive(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        LobbyUIManager.Instance.UpdateStatus("게임 입장 중...");
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("InGame");
    }

}
