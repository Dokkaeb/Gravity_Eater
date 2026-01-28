using UnityEngine;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }
    string _gameVersion = "1";
    float _nextUpdateTime = 0f;
    private string _currentStatus = "대기 중...";
    public string CurrentStatus => _currentStatus;

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    private void SetStatus(string msg)
    {
        _currentStatus = msg;
        LobbyUIManager.Instance.UpdateStatus(msg);
    }
    private async void Start()
    {
        SetStatus("시스템 초기화 중...");

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
            SetStatus("이름 입력 후 Start를 누르세요.");
            LobbyUIManager.Instance.SetJoinButtonActive(true);
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsConnectedAndReady && Time.time > _nextUpdateTime)
        {
            _nextUpdateTime = Time.time + 1f; //1초주기
            UpdateServerStatus();
        }
    }

    public void Connect(string nickName)
    {        
        PhotonNetwork.LocalPlayer.NickName = nickName;

        if (!PhotonNetwork.IsConnected) //서버연결 안된경우(처음실행)
        {
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            SetStatus("서버 접속 중...");
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

        SetStatus("빈 방 찾는 중...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("서버 연결 완료! start 누르세요");
        LobbyUIManager.Instance.SetJoinButtonActive(true);

        CleanUpDatabase();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("입장 가능한 빈 방이 없음. 새로운 방 생성 중...");

        // 최대 인원을 10명으로 설정
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 10,
            CleanupCacheOnLeave = true // 플레이어가 나갔을 때 생성한 오브젝트 삭제 (서버 최적화)
        };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        SetStatus("게임 입장 중...");
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("InGame");
    }

    private async void CleanUpDatabase()
    {
        if (FirebaseManager.Instance != null)
        {
            // 50위 밖 + 7일 지난 데이터 정리 실행
            await FirebaseManager.Instance.CleanOldData();
        }
    }

    private void UpdateServerStatus()
    {
        int totalPlayers = PhotonNetwork.CountOfPlayers; //전체접속자
        int totalRooms = PhotonNetwork.CountOfRooms; //방갯수

        LobbyUIManager.Instance.UpdateServerInfo(totalPlayers, totalRooms);
    }
}
