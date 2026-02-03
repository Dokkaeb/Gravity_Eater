using System.Collections;
using UnityEngine;

public class Nebula : MonoBehaviour
{
    public int DataIndex { get; private set; }
    public int NebulaId { get; private set; }
    public float RemainingLifeTime { get; private set; }
    public enum NebulaType { Red, Blue };
    NebulaData _data;
    SpriteRenderer _spr;
    float _lifeTime;
    float _currentRotateSpeed;

    private float _penaltyTimer = 0f; // 페널티 타이머
    [SerializeField] float _penaltyTickRate = 0.5f; // 0.5초마다 점수 감소

    private PlayerCtrl _affectedMinePlayer;

    private void Awake()
    {
        _spr = GetComponent<SpriteRenderer>();
    }
    public void Setup(NebulaData data, int dataIndex, int id, float lifeTime)
    {
        _data = data;
        this.DataIndex = dataIndex;
        this.NebulaId = id;
        this.RemainingLifeTime = lifeTime;
        _spr.sprite = data.sprite;

        float speed = Random.Range(30f, 60f);
        float direction = (Random.value > 0.5f) ? 1f : -1f;
        _currentRotateSpeed = speed * direction;

        StopAllCoroutines();
        StartCoroutine(NebulaAnimation(lifeTime));
    }
    private void Update()
    {
        transform.Rotate(0, 0, _currentRotateSpeed * Time.deltaTime);
        // 마스터만 시간을 깎는 게 아니라, 각자 로컬에서도 남은 시간을 갱신 (동기화 보조용)
        if (RemainingLifeTime > 0)
            RemainingLifeTime -= Time.deltaTime;
    }
    private IEnumerator NebulaAnimation(float lifeTime)
    {
        float t = 0;
        float animDuration = 0.5f;

        while (t < 1.0f)
        {
            t += Time.deltaTime * 2f;
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;


        float waitTime = lifeTime - (animDuration * 2);
        yield return new WaitForSeconds(Mathf.Max(0, waitTime));

        while (t > 0)
        {
            t -= Time.deltaTime * 2f;
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var player = collision.GetComponent<PlayerCtrl>();
            if (player != null && player.photonView.IsMine)
            {
                if (_data.type == NebulaType.Red)
                {
                    player.AddScore(-_data.scorePenalty);

                    //위치이동
                    Vector2 randomPos = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
                    player.transform.position = randomPos;

                    SoundManager.Instance.PlaySFX("sfx_Bite");

                    //스턴2초
                    player.ApplyStun(2.0f);

                    if (CamFollow.Instance != null)
                    {
                        CamFollow.Instance.FastMove(player.transform.position);
                        CamFollow.Instance.ShakeCam(1f, 2f);
                    }
                }
                else if (_data.type == NebulaType.Blue)
                {
                    // 파랑 진입 시 슬로우 적용 및 플레이어 참조 기록
                    _affectedMinePlayer = player;
                    _affectedMinePlayer.UpdateSlowState(true, _data.slowAmount);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_data.type == NebulaType.Blue && _affectedMinePlayer != null)
        {
            _penaltyTimer += Time.deltaTime;
            // 파랑은 머무는 동안 점수 지속 감소
            if (_penaltyTimer >= _penaltyTickRate)
            {
                _affectedMinePlayer.AddScore(-(_data.scorePenalty * _penaltyTickRate));
                _penaltyTimer = 0f;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_data.type == NebulaType.Blue && _affectedMinePlayer != null)
        {
            // 영역을 벗어나면 슬로우 해제
            if (collision.GetComponent<PlayerCtrl>() == _affectedMinePlayer)
            {
                _penaltyTimer = 0f;
                ClearEffect();
            }
        }
    }
    private void OnDisable()
    {
        ClearEffect();
    }

    private void ClearEffect()
    {
        if (_affectedMinePlayer != null)
        {
            _affectedMinePlayer.UpdateSlowState(false);
            _affectedMinePlayer = null;
        }
    }
}
