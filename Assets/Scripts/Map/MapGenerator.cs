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
    private List<GameObject> _activeItems = new List<GameObject>();

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
        yield return new WaitForSeconds(_foodRespawnTime);

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

        // 모든 클라이언트에게 생성 명령
        photonView.RPC(nameof(RPC_SpawnItem), RpcTarget.All, spawnPos, dataIdx);
    }

    IEnumerator Co_ItemSpawner()
    {
        yield return new WaitForSeconds(_itemSpawnInterval);

        while (true)
        {
            // 현재 맵에 깔린 아이템 개수 체크 (리스트에서 비활성화된 것은 제거)
            _activeItems.RemoveAll(item => item == null || !item.activeInHierarchy);

            // 최대 개수보다 적을 때만 생성
            if (_activeItems.Count < _maxItemOnMap)
            {
                int spawnBatch = Mathf.Min(3, _maxItemOnMap - _activeItems.Count);

                for (int i = 0; i < spawnBatch; i++)
                {
                    SpawnRandomItem();
                }
            }

            //다음 스폰까지 대기
            yield return new WaitForSeconds(_itemSpawnInterval);
        }
    }

    [PunRPC]
    private void RPC_SpawnItem(Vector3 pos,int dataIdx)
    {
        GameObject itemObj = PoolManager.Instance.Get(_itemPrefab);
        itemObj.transform.position = pos;
        itemObj.SetActive(true);

        itemObj.GetComponent<ItemObject>().Setup(_itemTypes[dataIdx]);

        // 생성된 아이템을 리스트에 추가하여 관리
        if (!_activeItems.Contains(itemObj))
        {
            _activeItems.Add(itemObj);
        }
    }

    public void RequestDestroyItem(Vector3 pos)
    {
        photonView.RPC(nameof(RPC_MasterDestroyItem), RpcTarget.MasterClient, pos);
    }
    [PunRPC]
    private void RPC_MasterDestroyItem(Vector3 pos)
    {
        // 마스터가 모두에게 삭제 명령
        photonView.RPC(nameof(RPC_FinalizeDestroyItem), RpcTarget.All, pos);
    }
    [PunRPC]
    private void RPC_FinalizeDestroyItem(Vector3 pos)
    {
        // 해당 위치에 있는 아이템을 찾아서 삭제 (또는 풀에 반환)
        // _activeItems 리스트에서 가장 가까운 아이템을 찾습니다.
        GameObject targetItem = null;
        float minDist = 0.5f; // 오차 범위

        foreach (var item in _activeItems)
        {
            if (item != null && Vector3.Distance(item.transform.position, pos) < minDist)
            {
                targetItem = item;
                break;
            }
        }

        if (targetItem != null)
        {
            _activeItems.Remove(targetItem);
            PoolManager.Instance.Release(targetItem);
        }
    }

    #endregion
}
