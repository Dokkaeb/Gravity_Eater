using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _playerPrefab;

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            OnJoinedRoom();
        }
    }

    //포톤 서버로부터 방 입장 신호받으면 실행
    public override void OnJoinedRoom()
    {
        Debug.Log($"방입장완료 현재상태 : {PhotonNetwork.NetworkClientState}");

        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();

            if (PhotonNetwork.IsMasterClient)
            {
                CreateMapSeed();
            }
            else
            {
                CheckExistingMapSeed();
            }
        }
    }
    private void SpawnPlayer()
    {
        float randomX = Random.Range(-10f, 10f);
        float randomY = Random.Range(-10f, 10f);
        Vector3 spawnPos = new Vector3(randomX, randomY, 0);

        PhotonNetwork.Instantiate("Prefabs/Player", spawnPos,Quaternion.identity);
        Debug.Log("플레이어 생성 완료");
    }

    private void CreateMapSeed()
    {
        int randomSeed = Random.Range(1, 999999);
        Hashtable props = new Hashtable { { "MapSeed", randomSeed } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log($"랜덤 시드생성 : {randomSeed}");
    }

    private void CheckExistingMapSeed()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MapSeed"))
        {
            int seed = (int)PhotonNetwork.CurrentRoom.CustomProperties["MapSeed"];
            MapGenerator.Instance.GenerateMap(seed);
            Debug.Log($"기존 맵 시드 발견: {seed}");
        }
    }

    //맵시드 업데이트될때 모든유저 공통콜백
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("MapSeed"))
        {
            int seed = (int)propertiesThatChanged["MapSeed"];
            MapGenerator.Instance.GenerateMap(seed);
            Debug.Log($"맵 시드 동기화 완료: {seed}");
        }
    }
}
