using UnityEngine;
using Photon.Pun;

public class FoodItem : MonoBehaviour
{
    [SerializeField] int _foodIndex;
    MapGenerator _mapGenerator;
    SpriteRenderer _spr;
    public float CurrentScore {  get; private set; }

    private void Awake()
    {
        _spr = GetComponent<SpriteRenderer>();
    }
    public void Initialize(int index,MapGenerator generator,FoodData data = null)
    {
        _foodIndex = index;
        _mapGenerator = generator;

        if(data != null)
        {
            CurrentScore = data.scoreAmount;
            if(data.foodSprite != null)
            {
                _spr.sprite = data.foodSprite;
            }
            transform.localScale = Vector3.one * data.scaleSize;
        }
        else
        {
            CurrentScore = 1f;
        }
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
