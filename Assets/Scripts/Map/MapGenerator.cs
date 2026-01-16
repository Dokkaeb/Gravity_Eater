using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    public static MapGenerator Instance;

    [Header("세팅")]
    [SerializeField] int _maxFoodOnMap = 1000;
    [SerializeField] float _mapSize = 100f;

    [Header("먹이 데이터")]
    [SerializeField] FoodData[] _foodTypes;
    [SerializeField] float _respawnTime = 10f;

    //먹이,전리품 활성화 추적용 딕셔너리
    private Dictionary<int,GameObject> _activeFoods = new Dictionary<int,GameObject>();

    //전리품 ID는 먹이랑 안곂치게 큰숫자부터 시작
    private int _lootIdCounter = 100000;
    private System.Random _prng;

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
    }
    private void SpawnFood(int id)
    {
        float x = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        float y = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        Vector3 pos = new Vector3(x, y, 0);

        CreateFood(id, pos);
    }

    private void CreateFood(int id, Vector3 pos)
    {
        GameObject food = PoolManager.Instance.Get();
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
    //플레이어가 먹으면 호출
    public void RequestEatFood(int id,int viewID)
    {
        //AllBuffered를 써서 나중에 들어온 유저도 뭘먹었는지 알수있게
        photonView.RPC(nameof(RPC_ProcessEat), RpcTarget.AllBuffered, id,viewID);
    }

    [PunRPC]
    private void RPC_ProcessEat(int id, int viewID)
    {
        if(_activeFoods.TryGetValue(id, out GameObject food))
        {
            float score = food.GetComponent<FoodItem>().CurrentScore; //먹이에서 점수정보 가져오기
            Vector3 lastPos = food.transform.position; //먹힌 위치 기억

            PoolManager.Instance.Release(food);
            _activeFoods.Remove(id);

            //먹은유저 점수추가
            AwardScore(viewID, score);

            //일반 먹이 일정시간후 재생성
            if(id< 100000 && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(Co_RespawnFood(id,lastPos));
            }
        }
    }

    //재생성 코루틴
    IEnumerator Co_RespawnFood(int id,Vector3 pos)
    {
        yield return new WaitForSeconds(_respawnTime);

        photonView.RPC(nameof(RPC_SpawnSpecificFood),RpcTarget.All,id, pos);
    }

    //특정위치에 먹이생성RPC
    [PunRPC]
    private void RPC_SpawnSpecificFood(int id,Vector3 pos)
    {
        CreateFood(id, pos);
    }

    //전리품 생성
    public void RequestSpawnLoot(Vector3 pos,float lootScore)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int count = Mathf.FloorToInt(lootScore);
            for(int i = 0; i < count; i++)
            {
                int newId = _lootIdCounter++; //전리품에 마스터가 id부여
                Vector2 offset = Random.insideUnitCircle * (1.5f + (count * 0.05f));
                Vector3 spawnPos = pos + (Vector3)offset;

                //올버퍼로 나중에 들어온 사람도 전리품 보이게
                photonView.RPC(nameof(RPC_SpawnLoot), RpcTarget.AllBuffered, newId, spawnPos);
            }
        }
        
    }

    [PunRPC]
    private void RPC_SpawnLoot(int id,Vector3 pos)
    {
        GameObject loot = PoolManager.Instance.Get();
        loot.transform.position = pos;
        loot.SetActive(true);

        loot.GetComponent<FoodItem>().Initialize(id, this, _foodTypes[0]); //1점짜리 소환
        _activeFoods[id] = loot;
    }



    private void AwardScore(int viewID,float amount)
    {
        PhotonView targetPlayer = PhotonView.Find(viewID);
        if (targetPlayer != null && targetPlayer.IsMine)
        {
            targetPlayer.GetComponent<PlayerCtrl>().AddScore(amount);
        }
    }
}
