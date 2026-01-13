using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using Photon.Pun;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    public static MapGenerator Instance;

    [Header("세팅")]
    [SerializeField] GameObject _foodPrefab;
    [SerializeField] int _maxFoodOnMap = 1000;
    [SerializeField] float _mapSize = 100f;

    //먹이 활성화 추적용 딕셔너리
    private Dictionary<int,GameObject> _activeFoods = new Dictionary<int,GameObject>();

    //내장 풀
    private IObjectPool<GameObject> _foodPool;
    private System.Random _prng;

    private void Awake()
    {
        Instance = this;
        _foodPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_foodPrefab, transform), //부족하면 생성
            actionOnGet: (obj) => obj.SetActive(true),             //풀에서 꺼낼때
            actionOnRelease: (obj) => obj.SetActive(false),        //풀에 넣을 때 
            actionOnDestroy: (obj) => Destroy(obj),                //풀에 넘칠 때 삭제
            defaultCapacity: _maxFoodOnMap,
            maxSize: 2000
            );
    }

    //시드를 받아와 맵 만들기
    public void GenerateMap(int seed)
    {
        _prng = new System.Random(seed);

        for(int i =0; i < _maxFoodOnMap; i++)
        {
           SpawnFoodByIndex(i);
        }
    }
    private void SpawnFoodByIndex(int index)
    {
        float x = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        float y = (float)(_prng.NextDouble() * 2 - 1) * _mapSize;
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject food = _foodPool.Get();
        food.transform.position = spawnPos;

        FoodItem item = food.GetComponent<FoodItem>();
        if (item != null)
        {
            item.Initialize(index, this);
        }
        else
        {
            Debug.LogError("Food 프리팹에 FoodItem 스크립트가 없습니다!");
        }

        // 이미 존재하는 키인지 확인 후 추가 (오류 방지)
        if (!_activeFoods.ContainsKey(index))
        {
            _activeFoods.Add(index, food);
        }
    }
    //플레이어가 먹으면 호출
    public void RequestEatFood(int index,int viewID)
    {
        photonView.RPC(nameof(RPC_ProcessEat), RpcTarget.AllBuffered, index,viewID);
    }

    [PunRPC]
    private void RPC_ProcessEat(int index, int viewID)
    {
        if(_activeFoods.TryGetValue(index, out GameObject food))
        {
            _foodPool.Release(food); //풀로 반환
            _activeFoods.Remove(index);

            //먹은유저 점수추가
            PhotonView targetPlayer = PhotonView.Find(viewID);
            if(targetPlayer != null && targetPlayer.IsMine)
            {
                targetPlayer.GetComponent<PlayerCtrl>().AddScore(1f);
            }
        }
    }

}
