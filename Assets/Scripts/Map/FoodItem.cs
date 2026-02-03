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

    //자석효과 설정
    private void Update()
    {
        if(PlayerCtrl.LocalPlayer != null && PlayerCtrl.LocalPlayer.IsMagnetActive)
        {
            float distance = Vector2.Distance(transform.position, PlayerCtrl.LocalPlayer.transform.position);

            // 플레이어의 반지름비례 자석범위 조정
            float playerRadius = PlayerCtrl.LocalPlayer.transform.localScale.x * 0.5f;
            float effectiveRange = PlayerCtrl.LocalPlayer.MagnetRange + playerRadius;

            if (distance <= effectiveRange)
            {
                float pullSpeed = 15f + (PlayerCtrl.LocalPlayer.transform.localScale.x * 2f);

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    PlayerCtrl.LocalPlayer.transform.position,
                    pullSpeed*Time.deltaTime
                    );
            }
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
                    SoundManager.Instance.PlaySFX("sfx_Pop");
                }
            }
        }
    }
}
