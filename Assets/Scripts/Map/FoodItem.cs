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

            if (pv != null && pv.IsMine)
            {
                if (_foodIndex != -1)
                {
                    // ¿œπ› ∏  ª˝º∫ ∏‘¿Ã
                    _mapGenerator.RequestEatFood(_foodIndex, pv.ViewID);
                }
                else
                {
                    // ¿¸∏Æ«∞ ∏‘¿Ã (¿Œµ¶Ω∫ æ¯¿Ω)
                    _mapGenerator.RequestEatLoot(transform.position, pv.ViewID);
                }
            }
        }
    }
}
