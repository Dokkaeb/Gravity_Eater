using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    public static MapGenerator Instance;

    #region 필드
    [Header("세팅")]
    [SerializeField] int _maxFoodOnMap = 1000; //맵에 유지할 먹이갯수
    [SerializeField] float _mapSize = 100f;

    [Header("프리팹 설정")]
    [SerializeField] GameObject _foodPrefab;
    [SerializeField] GameObject _nebulaPrefab;

    [Header("데이터")]
    [SerializeField] FoodData[] _foodTypes;
    [SerializeField] NebulaData[] _nebulaTypes;
    [SerializeField] float _respawnTime = 10f;  //먹이재생성시간
    [SerializeField] float _nebulaSpawnInterval = 15f;

    //먹이,전리품 활성화 추적용 딕셔너리
    private Dictionary<int,GameObject> _activeFoods = new Dictionary<int,GameObject>();

    //전리품 ID는 먹이랑 안곂치게 큰숫자부터 시작
    private int _lootIdCounter = 100000;
    private System.Random _prng;
    #endregion

    #region Initialization
    private void Awake()
    {
        Instance = this;
    }

    //시드를 받아와 맵 만들기
    public void GenerateMap(int seed)
    {
        _prng = new System.Random(seed);

        for(int i =0; i < _maxFoodOnMap; i++)
        {
           SpawnFood(i);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(Co_NebulaSpawner());
        }
    }
    #endregion

    #region 먹이생성 로직
    private void SpawnFood(int id)
    {
        float x = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        float y = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        Vector3 pos = new Vector3(x, y, 0);

        CreateFood(id, pos);
    }

    private void CreateFood(int id, Vector3 pos)
    {
        GameObject food = PoolManager.Instance.Get(_foodPrefab);
        food.transform.position = pos;
        food.SetActive(true);

        //확률로 먹이 다른거 스폰
        int rand = _prng.Next(0, 100);
        FoodData data = _foodTypes[0];                 // 기본 1점짜리
        if (rand > 98) data = _foodTypes[2];      // 2퍼확률 젤큰거
        else if (rand > 85) data = _foodTypes[1]; //15퍼확률 중간크기

        food.GetComponent<FoodItem>().Initialize(id, this, data);
        _activeFoods[id] = food;
    }
    #endregion

    #region 상호작용, 점수관련
    //플레이어가 먹으면 마스터한테 판정 요청
    public void RequestEatFood(int id,int viewID)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            return;
        }
        if (PhotonNetwork.NetworkClientState == ClientState.Leaving)
        {
            return;
        }
        
        photonView.RPC(nameof(RPC_MasterProcessEat), RpcTarget.MasterClient, id, viewID);
    }

    [PunRPC]
    private void RPC_MasterProcessEat(int id, int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (_activeFoods.ContainsKey(id))
        {
            // 마스터가 확인 후 모두에게 지우라고 명령
            photonView.RPC(nameof(RPC_FinalizeEat), RpcTarget.All, id, viewID);
        }
    }
    [PunRPC]
    private void RPC_FinalizeEat(int id, int viewID)
    {
        if (_activeFoods.TryGetValue(id, out GameObject food))
        {
            float score = food.GetComponent<FoodItem>().CurrentScore;
            Vector3 lastPos = food.transform.position;

            PoolManager.Instance.Release(food);
            _activeFoods.Remove(id);

            
            AwardScore(viewID, score);
            //일반먹이면 일정시간후 재생성
            if (id < 100000 && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(Co_RespawnFood(id, lastPos));
            }
        }
    }
    private void AwardScore(int viewID, float amount)
    {
        PhotonView targetPlayer = PhotonView.Find(viewID);
        if (targetPlayer != null && targetPlayer.IsMine)
        {
            targetPlayer.GetComponent<PlayerCtrl>().AddScore(amount);
        }
    }
    //먹이재생성 코루틴
    IEnumerator Co_RespawnFood(int id, Vector3 pos)
    {
        yield return new WaitForSeconds(_respawnTime);

        photonView.RPC(nameof(RPC_SpawnSpecificFood), RpcTarget.All, id, pos);
    }

    //특정위치에 먹이생성RPC
    [PunRPC]
    private void RPC_SpawnSpecificFood(int id, Vector3 pos)
    {
        CreateFood(id, pos);
    }
    #endregion


    #region 전리품 생성 로직
    //전리품 생성
    public void RequestSpawnLoot(Vector3 pos,float lootScore)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 점수가 100점이면 1점짜리 100개가 아니라, 10점짜리 10개로 변환 (개수 90% 절감)
        int valuePerLoot = 10;
        int totalCount = Mathf.CeilToInt(lootScore / valuePerLoot);
        if (totalCount <= 0) return;

        int sentCount = 0;
        while (sentCount < totalCount)
        {
            int batchCount = Mathf.Min(50, totalCount - sentCount); // 한 번에 최대 50개씩
            int[] ids = new int[batchCount];
            Vector3[] positions = new Vector3[batchCount];

            for (int i = 0; i < batchCount; i++)
            {
                ids[i] = _lootIdCounter++;
                positions[i] = pos + (Vector3)Random.insideUnitCircle * (2f + (totalCount * 0.05f));
            }

            photonView.RPC(nameof(RPC_SpawnLootBatch), RpcTarget.All, ids, positions);
            sentCount += batchCount;
        }

    }

    [PunRPC]
    private void RPC_SpawnLootBatch(int[] ids, Vector3[] positions)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (_activeFoods.ContainsKey(ids[i])) continue; // 이미 존재하면 스킵

            GameObject loot = PoolManager.Instance.Get(_foodPrefab);
            loot.transform.position = positions[i];
            loot.SetActive(true);

            // 10점짜리 데이터
            loot.GetComponent<FoodItem>().Initialize(ids[i], this, _foodTypes[2]);
            _activeFoods[ids[i]] = loot;
        }
    }
    #endregion

    #region 네트워크, 나중에 접속한사람
    //신규유저 접속시
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터가 새로 들어온 유저에게만 현재 활성화된 모든 전리품 정보를 보냄
            SendCurrentLootToNewPlayer(newPlayer);
        }
    }
    private void SendCurrentLootToNewPlayer(Player targetPlayer)
    {
        // 현재 활성화된 전리품 중 ID가 100000 이상인 것들만 추출
        var loots = _activeFoods.Where(kvp => kvp.Key >= 100000).ToList();
        if (loots.Count == 0) return;

        //key 와 position만 추출
        int[] ids = loots.Select(l => l.Key).ToArray();
        Vector3[] positions = loots.Select(l => l.Value.transform.position).ToArray();

        // RpcTarget.AllBuffered 대신 특정 유저(targetPlayer)에게만 전송
        photonView.RPC(nameof(RPC_SpawnLootBatch), targetPlayer, ids, positions);
    }

    #endregion

    #region 함정(네뷸라) 생성
    IEnumerator Co_NebulaSpawner()
    {
        yield return new WaitForSeconds(5f);

        while (true)
        {
            yield return new WaitForSeconds(_nebulaSpawnInterval);

            float x = Random.Range(-_mapSize, _mapSize);
            float y = Random.Range(-_mapSize, _mapSize);
            Vector3 spawnPos = new Vector3(x, y, 0);
            int dataIdx = Random.Range(0, _nebulaTypes.Length);

            photonView.RPC(nameof(RPC_SpawnNebula), RpcTarget.All, spawnPos, dataIdx);
        }
    }

    [PunRPC]
    private void RPC_SpawnNebula(Vector3 pos,int dataIdx)
    {
        GameObject nebulaObj = PoolManager.Instance.Get(_nebulaPrefab);
        nebulaObj.transform.position = pos;
        nebulaObj.SetActive(true);

        nebulaObj.GetComponent<Nebula>().Setup(_nebulaTypes[dataIdx]);
    }
    #endregion
}
