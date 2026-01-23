using System.Collections;
using UnityEngine;

public class Nebula : MonoBehaviour
{
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
    public void Setup(NebulaData data)
    {
        _data = data;
        _spr.sprite = data.sprite;

        float speed = Random.Range(30f, 60f);
        float direction = (Random.value > 0.5f) ? 1f : -1f;
        _currentRotateSpeed = speed * direction;

        _lifeTime = Random.Range(data.minDuration, data.maxDuration);
        StartCoroutine(NebulaLifeCycle());
    }
    private void Update()
    {
        transform.Rotate(0, 0, _currentRotateSpeed * Time.deltaTime);
    }
    private IEnumerator NebulaLifeCycle()
    {
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime * 2f;
            transform.localScale = Vector3.one * t;
            yield return null;
        }

        yield return new WaitForSeconds(_lifeTime);

        while (t > 0)
        {
            t -= Time.deltaTime * 2f;
            transform.localScale = Vector3.one * t;
            yield return null;
        }

        PoolManager.Instance.Release(gameObject);
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
                    Vector2 randomPos = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
                    player.transform.position = randomPos;
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
