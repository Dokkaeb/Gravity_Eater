using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Collections;
using DG.Tweening;

public class PlayerCtrl : MonoBehaviourPun, IPunObservable
{
    public static PlayerCtrl LocalPlayer;
    public enum PlayerState { Move, Dash ,Dead }

    [Header("기본세팅")]
    [SerializeField] PlayerState _currentState = PlayerState.Move;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _rotateSpeed = 10f;
    [SerializeField] float _acceleration = 20f; //가속
    bool _isInvincible = false;

    [Header("성장 세팅")]
    [SerializeField] float _currentScore = 0;
    [SerializeField] float _scaleMultiplier = 0.01f; //점수당 크기증가 비율
    [SerializeField] float _minScale = 0.3f;
    [SerializeField] float _maxScale = 2.5f;

    [Header("대시 세팅")]
    [SerializeField] float _dashMultiplier = 2.0f;
    [SerializeField] float _dashCostPerSecond = 5.0f;
    [SerializeField] GameObject _dashTrail;

    [Header("시각 효과")]
    [SerializeField] GameObject _asteroidsEffect;
    [SerializeField] Material _blackHole;
    [SerializeField] float _asteroidsRotationSpeed = 90f;
    [SerializeField] float _asteroidsTargetScore = 100f;
    [SerializeField] float _toStarTargetScore = 300f;
    [SerializeField] float _toBHTargetScore = 500f;
    bool _isAsteroids = false;
    bool _isStar = false;
    bool _isBlackHole = false;
    Tween _scaleTween;
    float _logicScale = 1f;

    [Header("행성스킨 SO")]
    [SerializeField] PlanetSkins _planetSkins;
    [SerializeField] PlanetSkins _starSkins;

    [Header("슬로우 설정")]
    float _slowMultiplier = 1f;

    [Header("아이템 설정")]
    [SerializeField] GameObject _shieldVisual;
    [SerializeField] GameObject _magnetVisual;
    bool _isShield = false;
    bool _isMagnetActive = false;
    float _magnetRange = 0f;
    float _boosterSpeed = 0f;
    Coroutine _shieldCoroutine;
    Coroutine _magnetCoroutine;
    Coroutine _boostCoroutine;

    public float SlowMultiplier => _slowMultiplier;
    public float MagnetRange => _magnetRange * (transform.localScale.x * 0.8f);
    public float LogicScale => _logicScale;
    public bool IsInvincible => _isInvincible;
    public bool IsMagnetActive => _isMagnetActive;

    Vector2 _mousePos;
    Rigidbody2D _rb;
    SpriteRenderer _spr;
    ParticleSystem _dashTrailParticle;
    Texture2D _currentSkinTex;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spr = GetComponent<SpriteRenderer>();

        if (_dashTrail != null)
        {
            _dashTrailParticle = _dashTrail.GetComponent<ParticleSystem>();
        }
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            LocalPlayer = this;
            StartCoroutine(Co_SpawnProtection(2f)); // 스폰 보호

            // 카메라 매니저에게 나를 타겟으로 설정하라고 알림
            if (CamFollow.Instance != null)
            {
                CamFollow.Instance.SetTarget(this.transform);
                UpdateScale();
            }

            int myIndex = PlayerPrefs.GetInt("SelectedPlanetIndex", 0);
            photonView.RPC(nameof(RPC_ApplySkin), RpcTarget.AllBuffered, myIndex);

            if (UIManager.Instance != null) //매니저에게 세팅요청
            {
                UIManager.Instance.ConnectScore(_currentScore);
            }
        }

    }
    
    IEnumerator Co_SpawnProtection(float duration)
    {
        _isInvincible = true;
        Debug.Log("잠깐 무적");

        //반투명
        if(_spr != null)
        {
            Color c = _spr.color;
            c.a = 0.5f;
            _spr.color = c;
        }

        yield return new WaitForSeconds(duration);

        _isInvincible = false;

        if (_spr != null)
        {
            Color c = _spr.color;
            c.a = 1f;
            _spr.color = c;
        }
        Debug.Log("보호끝");
    }
    private void OnDestroy()
    {
        if (photonView.IsMine && LocalPlayer == this)
        {
            LocalPlayer = null;
        }
    }

    [PunRPC]
    private void RPC_ApplySkin(int index)
    {
        if (_spr != null && _planetSkins != null && index < _planetSkins.sprites.Length)
        {
            _spr.sprite = _planetSkins.sprites[index];
            _currentSkinTex = _spr.sprite.texture;
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || _currentState == PlayerState.Dead) return;

        HandleRotation();

        switch( _currentState)
        {
            case PlayerState.Move:
                HandleMovement(_moveSpeed);
                break;
            case PlayerState.Dash:
                HandleDash();
                break;
        }

        UpdateVisualEffects();
    }

    public void OnDashInput(InputAction.CallbackContext ctx)
    {
        if(!photonView.IsMine || _currentState == PlayerState.Dead) return;

        if(ctx.started && _currentScore > 0)
        {
            ChangeState(PlayerState.Dash);
        }
        else if (ctx.canceled)
        {
            ChangeState(PlayerState.Move);
        }
    }
    public void ChangeState(PlayerState newState)
    {
        _currentState = newState;
        Debug.Log("현재 상태: " + newState);

        bool isDashing = (_currentState == PlayerState.Dash);
        photonView.RPC(nameof(RPC_UpdateDashEffect), RpcTarget.All, isDashing);

        if (photonView.IsMine)
        {
            if (isDashing)
            {
                // 나에게만 즉시 들리는 사운드
                SoundManager.Instance?.PlaySFX("sfx_Dash_Start");
            }
            else
            {
                SoundManager.Instance?.PlaySFX("sfx_Dash_End");
            }
        }

    }
    [PunRPC]
    private void RPC_UpdateDashEffect(bool isDashing)
    {
        if(_dashTrail != null)
        {
            _dashTrail.SetActive(isDashing);
        }
    }

    //마우스 방향 바라보기
    private void HandleRotation()
    {
        if (Mouse.current == null) return;

        //마우스 위치 좌표 계산
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        _mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        //내위치에서 마우스쪽으로 방향벡터 계산
        Vector2 dir = (_mousePos - (Vector2)transform.position).normalized;

        //기존에 있던 마우스 위치에 도달해도 빙빙안돌게하기(0.5f 이내면 회전 업데이트 안 함)
        if (dir == Vector2.zero || Vector2.Distance(_mousePos, transform.position) < 0.5f)
        {
            return;
        }

        //회전계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);

        //덩치에 따른 회전속도 보정
        float currentRotateSpeed = _rotateSpeed / (transform.localScale.x * 0.5f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            currentRotateSpeed * 10f * Time.fixedDeltaTime
            );
       
    }

    //이동
    private void HandleMovement(float speed)
    {
        float finalSpeed = (speed + _boosterSpeed) * SlowMultiplier;
        Vector2 targetVelocity = transform.up * finalSpeed;

        _rb.linearVelocity = Vector2.MoveTowards(
        _rb.linearVelocity,
        targetVelocity,
        _acceleration * Time.fixedDeltaTime
        );
    }
    //대시
    private void HandleDash()
    {
        if (_currentScore > 0)
        {

            float scaleCost = _dashCostPerSecond * transform.localScale.x;

            _currentScore -= scaleCost * Time.deltaTime;

            if (_currentScore < 0) _currentScore = 0;

            UpdateScale();
            UIManager.Instance?.UpdatePlayerScore(_currentScore);
            HandleMovement(_moveSpeed * _dashMultiplier);
        }
        else
        {
            ChangeState(PlayerState.Move);
        }
    }

    public void AddScore(float scoreAmount)
    {
        if (!photonView.IsMine) return;

        _currentScore += scoreAmount;

        if (_currentScore < 0) _currentScore = 0;

        UpdateScale();

        if (scoreAmount > 0)
        {
            _scaleTween?.Kill(true);
            _scaleTween = transform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 3, 0.5f);
        }

        if (UIManager.Instance != null) //UI 갱신 보고
        {
            UIManager.Instance.UpdatePlayerScore(_currentScore);
        }
    }
  

    private void UpdateScale()
    {
        float calculatedScale = _minScale + (_currentScore * _scaleMultiplier);
        float finalScale = Mathf.Clamp(calculatedScale, _minScale, _maxScale);
        _logicScale = finalScale; //카메라 줌용 수치저장
        //크기설정
        transform.localScale = new Vector3(finalScale, finalScale, 1f);

        // 소행성 벨트 크기 유지
        if (_asteroidsEffect != null)
        {
            _asteroidsEffect.transform.localScale = Vector3.one * 3.3f;
        }

        //대시 이펙트 파티클 조절
        if (_dashTrailParticle != null)
        {
            var shape = _dashTrailParticle.shape;
            var main = _dashTrailParticle.main;

            shape.radius = (finalScale * 2.5f) + 1.3f;
            main.startSize = finalScale * 2.5f;
        }

    }

    public void UpdateSlowState(bool isSlow,float multiplier = 1f)
    {
        if (isSlow)
        {
            _slowMultiplier = multiplier;
        }
        else
        {
            _slowMultiplier = 1f;
        }
    }

    //포톤 데이터 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_currentScore); // 내점수 서버로 전송
        }
        else
        {
            // 다른플레이어 점수 수신하여 크기 업데이트
            this._currentScore = (float)stream.ReceiveNext();
            UpdateScale();
        }
    }

    public void OnHeadHitOhterPlayer(PlayerCtrl otherBody)
    {
        if(!photonView.IsMine || _currentState == PlayerState.Dead) return;

        if (_isInvincible) return; //무적이면 안죽게

        if (otherBody != null && otherBody.IsInvincible)
        {
            Debug.Log("상대방이 스폰 보호 중이라 충돌 무시");
            return;
        }

        Debug.Log("머리가 상대 몸통에 닿았습니다");
        OnDeath();
    }

    private void OnDeath()
    {
        // 실드가 있으면 사망 절차를 밟지 않고 실드만 파괴
        if (_isShield)
        {
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = null;

            _isShield = false;
            Debug.Log("실드 방어로 생존");
            if (_shieldVisual != null) _shieldVisual.SetActive(false);
            StartCoroutine(Co_SpawnProtection(0.5f)); //잠깐 무적주기
            return;
        }

        if(photonView.IsMine && CamFollow.Instance != null)
        {
            CamFollow.Instance.ShakeCam(1f, 2f);
        }

        _currentState = PlayerState.Dead;
        _rb.linearVelocity = Vector2.zero;

        ResetAllItemEffects();

        if (UIManager.Instance != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("sfx_Lose");
            UIManager.Instance.ShowDeathUI(_currentScore);
        }

        float lootAmount = _currentScore * 0.5f; //점수 50퍼 전리품으로
        MapGenerator.Instance?.RequestSpawnLoot(transform.position, lootAmount); //전리품생성

        photonView.RPC(nameof(RPC_OnDead), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_OnDead()
    {
        gameObject.SetActive(false);
    }

    private void ResetAllItemEffects()
    {
        // 코루틴 중단
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
        if (_magnetCoroutine != null) StopCoroutine(_magnetCoroutine);
        if (_boostCoroutine != null) StopCoroutine(_boostCoroutine);

        _shieldCoroutine = null;
        _magnetCoroutine = null;
        _boostCoroutine = null;

        // 상태 변수 초기화
        _isShield = false;
        _isMagnetActive = false;
        _magnetRange = 0f;
        _boosterSpeed = 0f;

        // 비주얼 초기화
        if (_shieldVisual != null) _shieldVisual.SetActive(false);
        if (_magnetVisual != null) _magnetVisual.SetActive(false);

        // 사운드 중단 (자석 루프 사운드 등)
        SoundManager.Instance?.StopLoopSFX();
    }

    private void UpdateVisualEffects()
    {
        if (_asteroidsEffect == null) return;

        // 점수에 따른 상태 업데이트
        bool shouldBeAsteroids = (_currentScore >= _asteroidsTargetScore);

        if (_isAsteroids != shouldBeAsteroids)
        {
            _isAsteroids = shouldBeAsteroids;

            if (_isAsteroids)
            {
                _asteroidsEffect.SetActive(true);
                _asteroidsEffect.transform.DOKill(); // 0에서 시작
                _asteroidsEffect.transform.localScale = Vector3.zero;

                DOVirtual.DelayedCall(0.1f, () => {
                    // 딜레이 후 애니메이션 실행
                    if (_isAsteroids)
                    {
                        _asteroidsEffect.transform.DOScale(Vector3.one * 3.3f, 0.3f).SetEase(Ease.OutBack);
                    }
                });
            }
            else
            {
                _asteroidsEffect.transform.DOKill();
                // 사라질 때도 스르륵
                _asteroidsEffect.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    // 애니메이션이 완전히 끝난 후 비활성화
                    if (!_isAsteroids) _asteroidsEffect.SetActive(false);
                });
            }
        }
        if (_isAsteroids && _asteroidsEffect.activeSelf)
        {
            _asteroidsEffect.transform.Rotate(0, 0, _asteroidsRotationSpeed * Time.deltaTime);
        }

        if (_starSkins != null && !_isStar)
        {
            if(_currentScore >= _toStarTargetScore)
            {
                ChangeToStar();
            }
        }
        if(_blackHole != null && !_isBlackHole)
        {
            if(_currentScore >= _toBHTargetScore)
            {
                ChangeToBlackHole();
            }
        }
    }

    private void ChangeToStar()
    {
        _isStar = true;

        transform.DOKill();

        //응축
        transform.DOScale(_logicScale * 0.1f, 0.7f).SetEase(Ease.InBack).OnComplete(() => {

            //랜덤 스킨 교체
            int randomIndex = Random.Range(0, _starSkins.sprites.Length);
            photonView.RPC(nameof(RPC_ApplyStarSkin), RpcTarget.AllBuffered, randomIndex);

            // 터지는 연출
            transform.DOScale(_logicScale * 1.5f, 0.2f).SetEase(Ease.OutExpo).OnComplete(() => {
                
                //크기복구
                transform.DOScale(_logicScale, 1f).SetEase(Ease.OutElastic);
            });

            if(SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("sfx_PowerUp");
            }
        });
    }

    [PunRPC]
    private void RPC_ApplyStarSkin(int index)
    {
        if (_spr != null && _starSkins != null && index < _starSkins.sprites.Length)
        {
            _spr.sprite = _starSkins.sprites[index];
        }
    }

    private void ChangeToBlackHole()
    {
        _isBlackHole = true;
        transform.DOKill();

        // 응축
        transform.DOScale(_logicScale * 0.1f, 0.7f).SetEase(Ease.InBack).OnComplete(() => {
            //블랙홀로 변경
            photonView.RPC(nameof(RPC_ApplyBlackHole), RpcTarget.AllBuffered);
            // 터지는 연출
            transform.DOScale(_logicScale * 1.5f, 0.2f).SetEase(Ease.OutExpo).OnComplete(() => {
                
                //원래크기 복구
                transform.DOScale(_logicScale, 1f).SetEase(Ease.OutBack);
            });

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("sfx_PowerUp");
            }
        });
    }

    [PunRPC]
    private void RPC_ApplyBlackHole()
    {
        if(_spr != null && _blackHole != null)
        {
            _spr.material = new Material(_blackHole);

            _spr.material.SetTexture("_Noise_Texture", _currentSkinTex);
        }
    }

    public void ApplyItemEffect(ItemData data)
    {
        if (data == null) return;

        Debug.Log($"아이템 획득 : {data.itemName}");

        switch (data.itemType)
        {
            case ItemType.Shield:
                if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
                _shieldCoroutine = StartCoroutine(Co_ShieldEffect(data.duration));
                break;
            case ItemType.Magnet:
                if (_magnetCoroutine != null) StopCoroutine(_magnetCoroutine);
                _magnetCoroutine = StartCoroutine(Co_MagnetEffect(data.duration, data.amount));
                break;

            case ItemType.Boost:
                if (_boostCoroutine != null) StopCoroutine(_boostCoroutine);
                _boostCoroutine = StartCoroutine(Co_BoostEffect(data.duration, data.amount));
                if (photonView.IsMine && CamFollow.Instance != null)
                {
                    CamFollow.Instance.ShakeCam(0.2f, 1f);
                }
                break;
        }
    }
    IEnumerator Co_ShieldEffect(float duration)
    {
        _isShield = true;
        if (_shieldVisual != null) _shieldVisual.SetActive(true);

        SoundManager.Instance.PlaySFX("sfx_Shield_Start");

        yield return new WaitForSeconds(duration * 0.8f);

        float timeLeft = duration * 0.2f;
        float blinkInterval = 0.1f; // 깜빡임 속도

        while (timeLeft > 0)
        {
            if (_shieldVisual != null)
            {
                // 실드 비주얼의 활성화 상태를 반전시켜 깜빡이게 함
                _shieldVisual.SetActive(!_shieldVisual.activeSelf);
            }

            yield return new WaitForSeconds(blinkInterval);
            timeLeft -= blinkInterval;
        }


        _isShield = false;
        if (_shieldVisual != null) _shieldVisual.SetActive(false);
        SoundManager.Instance.PlaySFX("sfx_Shield_Stop");

        _shieldCoroutine = null;
        
    }
    IEnumerator Co_MagnetEffect(float duration, float range)
    {
        _isMagnetActive = true;
        _magnetRange = range;
        if (_magnetVisual != null) _magnetVisual.SetActive(true);

        SoundManager.Instance.PlayLoopSFX("sfx_Magnet");

        yield return new WaitForSeconds(duration * 0.8f);

        float timeLeft = duration * 0.2f;
        float blinkInterval = 0.1f; // 깜빡임 속도

        while (timeLeft > 0)
        {
            if (_magnetVisual != null)
            {
                // 실드 비주얼의 활성화 상태를 반전시켜 깜빡이게 함
                _magnetVisual.SetActive(!_magnetVisual.activeSelf);
            }

            yield return new WaitForSeconds(blinkInterval);
            timeLeft -= blinkInterval;
        }


        _isMagnetActive = false;
        if (_magnetVisual != null) _magnetVisual.SetActive(false);
        SoundManager.Instance.StopLoopSFX();

        _magnetCoroutine = null;
    }
    IEnumerator Co_BoostEffect(float duration, float speedAmount)
    {
        SoundManager.Instance.PlaySFX("sfx_Booster");
        _boosterSpeed = speedAmount;
        yield return new WaitForSeconds(duration);
        _boosterSpeed = 0f;
        _boostCoroutine = null;
    }
}
