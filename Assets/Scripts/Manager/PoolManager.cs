using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    [SerializeField] GameObject _foodPrefab;
    List<GameObject> _pool = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    // 풀에서 하나 꺼내오기
    public GameObject Get()
    {
        foreach (var obj in _pool)
        {
            if (!obj.activeSelf) return obj;
        }

        // 비활성화된 게 없으면 생성
        GameObject newObj = Instantiate(_foodPrefab, transform);
        _pool.Add(newObj);
        return newObj;
    }

    // 풀로 돌려보내기
    public void Release(GameObject obj)
    {
        obj.SetActive(false);
    }
}
