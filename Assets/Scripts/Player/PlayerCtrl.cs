using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Collections;

public class PlayerCtrl : MonoBehaviourPun, IPunObservable
{
    public static PlayerCtrl LocalPlayer;
    public enum PlayerState { Move, Dash ,Dead }

    [Header("기본세팅")]
    [SerializeField] PlayerState _currentState = PlayerState.Move;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _rotateSpeed = 10f;
    [SerializeField] float _acceleration = 10f; //가속
    bool _isInvincible = false;

    [Header("성장 세팅")]
    [SerializeField] float _currentScore = 0;
    [SerializeField] float _scaleMultiplier = 0.1f; //점수당 크기증가 비율
    [SerializeField] float _minScale = 1.0f;
    [SerializeField] float _maxScale = 2.5f;

    [Header("대시 세팅")]
    [SerializeField] float _dashMultiplier = 2.0f;
    [SerializeField] float _dashCostPerSecond = 2.0f;
    [SerializeField] GameObject _dashTrail;

    [Header("시각 효과")]
    [SerializeField] GameObject _dustEffect;
    [SerializeField] Material _blackHole;
    [SerializeField] float _dustRotationSpeed = 20f;
    [SerializeField] float _dustTargetScore = 100f;
    [SerializeField] float _toStarTargetScore = 300f;
    [SerializeField] float _toBHTargetScore = 500f;
    bool _isStar = false;
    bool _isBlackHole = false;

    [Header("행성스킨 SO")]
    [SerializeField] PlanetSkins _planetSkins;
    [SerializeField] PlanetSkins _starSkins;

    [Header("슬로우 설정")]
    float _slowMultiplier = 1f;

    public float SlowMultiplier => _slowMultiplier;
    public bool IsInvincible => _isInvincible;

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
            StartCoroutine(Co_SpawnProtection()); // 스폰 보호
            // 카메라 매니저에게 나를 타겟으로 설정하라고 알림
            if (CamFollow.Instance != null)
            {
                CamFollow.Instance.SetTarget(this.transform);
            }

            int myIndex = PlayerPrefs.GetInt("SelectedPlanetIndex", 0);
            photonView.RPC(nameof(RPC_ApplySkin), RpcTarget.AllBuffered, myIndex);

            if (UIManager.Instance != null) //매니저에게 세팅요청
            {
                UIManager.Instance.ConnectScore(_currentScore);
            }
        }

    }
    
    IEnumerator Co_SpawnProtection()
    {
        _isInvincible = true;
        Debug.Log("스폰보호시작 2초");

        //반투명
        if(_spr != null)
        {
            Color c = _spr.color;
            c.a = 0.5f;
            _spr.color = c;
        }

        yield return new WaitForSeconds(2f); //2초대기

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
        Vector2 targetVelocity = transform.up * (speed * SlowMultiplier);

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
            _currentScore -= _dashCostPerSecond * Time.deltaTime;

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

        UpdateScale();
        
        if(UIManager.Instance != null) //UI 갱신 보고
        {
            UIManager.Instance.UpdatePlayerScore(_currentScore);
        }
    }
  

    private void UpdateScale()
    {
        float calculatedScale = _minScale + (_currentScore * _scaleMultiplier);
        float finalScale = Mathf.Clamp(calculatedScale, _minScale, _maxScale);
        transform.localScale = new Vector3(finalScale, finalScale, 1f);

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
        _currentState = PlayerState.Dead;
        _rb.linearVelocity = Vector2.zero;

        if (UIManager.Instance != null)
        {
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

    private void UpdateVisualEffects()
    {
        if(_dustEffect != null)
        {
            if(_currentScore >= _dustTargetScore)
            {
                _dustEffect.SetActive(true);
                _dustEffect.transform.Rotate(0, 0, _dustRotationSpeed * Time.deltaTime);
            }
        }
        if(_starSkins != null && !_isStar)
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

        int randomIndex = Random.Range(0,_starSkins.sprites.Length);

        photonView.RPC(nameof(RPC_ApplyStarSkin), RpcTarget.AllBuffered, randomIndex);
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
        photonView.RPC(nameof(RPC_ApplyBlackHole),RpcTarget.AllBuffered);
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
}
