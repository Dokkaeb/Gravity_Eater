using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    Dictionary<GameObject, List<GameObject>> _pools = new Dictionary<GameObject, List<GameObject>>();

    private void Awake()
    {
        Instance = this;
    }

    // 풀에서 하나 꺼내오기
    public GameObject Get(GameObject prefab)
    {
        if (!_pools.ContainsKey(prefab))
            _pools.Add(prefab, new List<GameObject>());

        foreach (var obj in _pools[prefab])
        {
            if (!obj.activeSelf)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab, transform);
        _pools[prefab].Add(newObj);
        return newObj;
    }

    // 풀로 돌려보내기
    public void Release(GameObject obj)
    {
        obj.SetActive(false);
    }
}
