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
    [SerializeField] int _maxItemOnMap = 10;
    [SerializeField] int initialItemCount = 5;

    [Header("프리팹 설정")]
    [SerializeField] GameObject _foodPrefab;
    [SerializeField] GameObject _nebulaPrefab;
    [SerializeField] GameObject _itemPrefab;

    [Header("데이터")]
    [SerializeField] FoodData[] _foodTypes;
    [SerializeField] NebulaData[] _nebulaTypes;
    [SerializeField] ItemData[] _itemTypes;
    [SerializeField] float _foodRespawnTime = 10f;  
    [SerializeField] float _nebulaSpawnInterval = 15f;
    [SerializeField] float _itemSpawnInterval = 10f;


    //먹이,전리품 활성화 추적용 딕셔너리
    private Dictionary<int,GameObject> _activeFoods = new Dictionary<int,GameObject>();

    //아이템 추적용 리스트
    private Dictionary<int,GameObject> _activeItemDict = new Dictionary<int, GameObject>();

    private HashSet<int> _eatenFoodIds = new HashSet<int>(); //늦게들어온 유저가 비활성화시킬 이미 먹은 먹이들

    //전리품 ID는 먹이랑 안곂치게 큰숫자부터 시작
    private int _lootIdCounter = 100000;
    private int _itemIdCounter = 500000; //아이템 아이디
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
            for (int i = 0; i < initialItemCount; i++)
            {
                SpawnRandomItem(); // 랜덤 위치 아이템 생성 함수 호출
            }
            StartCoroutine(Co_NebulaSpawner());
            StartCoroutine(Co_ItemSpawner());
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
        //로컬에서 먼저 없애버리기
        if (_activeFoods.TryGetValue(id, out GameObject food))
        {
            // 시각적으로만 먼저 꺼버림 (고스트 현상 방지)
            food.SetActive(false);
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

            //일반먹이가 먹혔다는거 기록용
            if (id < 100000)
            {
                _eatenFoodIds.Add(id);
            }

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
        yield return new WaitForSeconds(_foodRespawnTime);

        photonView.RPC(nameof(RPC_SpawnSpecificFood), RpcTarget.All, id, pos);
    }

    //특정위치에 먹이생성RPC
    [PunRPC]
    private void RPC_SpawnSpecificFood(int id, Vector3 pos)
    {
        //먹이 다시생성됬으니 목록에서 제거
        if (id < 100000)
        {
            _eatenFoodIds.Remove(id);
        }

        CreateFood(id, pos);
    }
    #endregion


    #region 전리품 생성 로직
    //전리품 생성
    public void RequestSpawnLoot(Vector3 pos,float lootScore)
    {
        //마스터한테 요청
        photonView.RPC(nameof(RPC_RequestLootSpawnByMaster), RpcTarget.MasterClient, pos, lootScore);
    }
    [PunRPC]
    private void RPC_RequestLootSpawnByMaster(Vector3 pos, float lootScore)
    {
        // 여기는 마스터 클라이언트의 컴퓨터에서만 실행됨
        if (!PhotonNetwork.IsMasterClient) return;

        // 1점미만 생성안함
        if (lootScore < 1f) return;
        //10점단위로 점수 쪼개기
        int valuePerLoot = 10;
        int count = Mathf.Max(1, Mathf.FloorToInt(lootScore / valuePerLoot));

        // 기존의 생성 로직 진행...
        int sentCount = 0;
        while (sentCount < count)
        {
            int batchCount = Mathf.Min(50, count - sentCount);
            int[] ids = new int[batchCount];
            Vector3[] positions = new Vector3[batchCount];

            for (int i = 0; i < batchCount; i++)
            {
                ids[i] = _lootIdCounter++;
                // 사방으로 퍼지는 범위 조절 (갯수가 많을수록 더 넓게)
                float spreadRange = 2f + (count * 0.05f);
                positions[i] = pos + (Vector3)Random.insideUnitCircle * spreadRange;
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

            //지금 먹혀서 없는 일반먹이id목록 넘기기
            int[] eatenIds = _eatenFoodIds.ToArray();
            photonView.RPC(nameof(RPC_SyncEatenFoods), newPlayer, eatenIds);
        }
    }

    [PunRPC]
    private void RPC_SyncEatenFoods(int[] eatenIds)
    {
        foreach (int id in eatenIds)
        {
            if (_activeFoods.TryGetValue(id, out GameObject food))
            {
                food.SetActive(false); // 이미 누군가 먹은 것이므로 끔                                
            }
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

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"방장이 변경됨{newMasterClient.NickName}");

        if (newMasterClient.IsLocal)
        {
            StopAllCoroutines();

            //함정,아이템 재시작
            StartCoroutine(Co_NebulaSpawner());
            StartCoroutine(Co_ItemSpawner());
        }
    }

    #endregion

    #region 함정(네뷸라) 생성

    IEnumerator Co_NebulaSpawner()
    {
        yield return new WaitForSeconds(5f);

        while (true)
        {
            yield return new WaitForSeconds(_nebulaSpawnInterval);

            int spawnCount = 5;

            for (int i = 0; i < spawnCount; i++)
            {
                float x = Random.Range(-_mapSize, _mapSize);
                float y = Random.Range(-_mapSize, _mapSize);
                Vector3 spawnPos = new Vector3(x, y, 0);

                int dataIdx = Random.Range(0, _nebulaTypes.Length);

                // 마스터가 모든 클라이언트에게 개별적으로 생성 명령 전송
                photonView.RPC(nameof(RPC_SpawnNebula), RpcTarget.All, spawnPos, dataIdx);
            }
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

    #region 아이템 생성, 파괴

    private void SpawnRandomItem()
    {
        float x = Random.Range(-_mapSize, _mapSize);
        float y = Random.Range(-_mapSize, _mapSize);
        Vector3 spawnPos = new Vector3(x, y, 0);

        int dataIdx = Random.Range(0, _itemTypes.Length);

        int newId = _itemIdCounter++;

        // 모든 클라이언트에게 생성 명령
        photonView.RPC(nameof(RPC_SpawnItem), RpcTarget.All, spawnPos, dataIdx, newId);
    }

    IEnumerator Co_ItemSpawner()
    {
        yield return new WaitForSeconds(_itemSpawnInterval);

        while (true)
        {
            // 딕셔너리에서 비활성화된(이미 먹힌) 데이터 정리
            var keysToRemove = _activeItemDict
                .Where(kvp => kvp.Value == null || !kvp.Value.activeInHierarchy)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove) _activeItemDict.Remove(key);

            if (PhotonNetwork.IsMasterClient && _activeItemDict.Count < _maxItemOnMap)
            {
                SpawnRandomItem();
            }

            //다음 스폰까지 대기
            yield return new WaitForSeconds(_itemSpawnInterval);
        }
    }

    [PunRPC]
    private void RPC_SpawnItem(Vector3 pos,int dataIdx,int itemId)
    {
        GameObject itemObj = PoolManager.Instance.Get(_itemPrefab);
        itemObj.transform.position = pos;
        itemObj.SetActive(true);

        itemObj.GetComponent<ItemObject>().Setup(_itemTypes[dataIdx],itemId);

        //딕셔너리에 등록
        _activeItemDict[itemId] = itemObj;
    }

    public void RequestDestroyItem(int itemId)
    {
        photonView.RPC(nameof(RPC_MasterDestroyItem), RpcTarget.MasterClient, itemId);
    }
    [PunRPC]
    private void RPC_MasterDestroyItem(int itemId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // 마스터가 모두에게 삭제 명령
        if (_activeItemDict.ContainsKey(itemId))
        {
            photonView.RPC(nameof(RPC_FinalizeDestroyItem), RpcTarget.All, itemId);
        }
    }
    [PunRPC]
    private void RPC_FinalizeDestroyItem(int itemId)
    {
        if (_activeItemDict.TryGetValue(itemId, out GameObject targetItem))
        {
            _activeItemDict.Remove(itemId);
            PoolManager.Instance.Release(targetItem);
        }
    }

    #endregion
}
