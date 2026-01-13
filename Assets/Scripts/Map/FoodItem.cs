using UnityEngine;
using Photon.Pun;

public class FoodItem : MonoBehaviour
{
    [SerializeField] int _foodIndex;
    private MapGenerator _mapGenerator;

    public void Initialize(int index,MapGenerator generator)
    {
        _foodIndex = index;
        _mapGenerator = generator;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();

            if (MapGenerator.Instance != null)
            {
                MapGenerator.Instance.RequestEatFood(_foodIndex, pv.ViewID);
            }
            else
            {
                Debug.LogError("씬에 MapGenerator 인스턴스가 없습니다!");
            }
        }
    }
}
