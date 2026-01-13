using UnityEngine;
using Photon.Pun;

public class FoodItem : MonoBehaviour
{
    public int FoodIndex {  get; private set; }
    private MapGenerator _mapGenerator;

    public void Initialize(int index,MapGenerator generator)
    {
        FoodIndex = index;
        _mapGenerator = generator;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine)
            {
                //¿Ã ¿Œµ¶Ω∫¿« ∏‘¿Ã∏¶ ∏‘æ˙¥Ÿ∞Ì ∫∏∞Ì
                _mapGenerator.RequestEatFood(FoodIndex);
            }
        }
    }
}
