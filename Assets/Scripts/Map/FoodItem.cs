using UnityEngine;
using Photon.Pun;

public class FoodItem : MonoBehaviour
{
    [SerializeField] int _foodID;
    MapGenerator _mapGenerator;
    SpriteRenderer _spr;
    public float CurrentScore {  get; private set; }

    private void Awake()
    {
        _spr = GetComponent<SpriteRenderer>();
    }
    public void Initialize(int id,MapGenerator generator,FoodData data = null)
    {
        _foodID = id;
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
            //데이터가 없으면 기본세팅
            CurrentScore = 1f;
            transform.localScale = Vector3.one;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PhotonNetwork.InRoom && other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();

            if (pv != null && pv.IsMine)
            {
                if(_mapGenerator != null)
                {
                    _mapGenerator.RequestEatFood(_foodID,pv.ViewID);
                }
            }
        }
    }
}
