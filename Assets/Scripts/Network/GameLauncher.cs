using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class GameLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField] string _gameVersion = "1";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true; //네트워크연결유지
    }
    private void Start()
    {
        Debug.Log("서버접속시작");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = _gameVersion;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결완료");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 접속 성공");
        //무작위 방입장 없으면 생성
        PhotonNetwork.JoinOrCreateRoom("newRoom", new RoomOptions { MaxPlayers = 10 }, TypedLobby.Default);

    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"룸 입장 : {PhotonNetwork.CurrentRoom.Name}");

        if (PhotonNetwork.IsMasterClient)
        {
            int randomSeed = Random.Range(1, 999999);
            Hashtable props = new Hashtable { { "MapSeed", randomSeed } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"랜덤시드 생성 : {randomSeed}");
        }
    }
    //늦게 들어온 유저용 맵생성
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("MapSeed"))
        {
            int seed = (int)propertiesThatChanged["MapSeed"];
            MapGenerator.Instance.GenerateMap(seed);
        }
    }
}
