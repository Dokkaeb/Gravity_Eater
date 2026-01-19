using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerCtrl : MonoBehaviourPun, IPunObservable
{
    public enum PlayerState { Move, Dash ,Dead }

    [Header("기본세팅")]
    [SerializeField] PlayerState _currentState = PlayerState.Move;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _rotateSpeed = 10f;

    [Header("성장 세팅")]
    [SerializeField] float _currentScore = 0;
    [SerializeField] float _scaleMultiplier = 0.1f; //점수당 크기증가 비율
    [SerializeField] float _minScale = 1.0f;
    [SerializeField] float _maxScale = 2.5f;

    [Header("대시 세팅")]
    [SerializeField] float _dashMultiplier = 2.0f;
    [SerializeField] float _dashCostPerSecond = 2.0f;

    [Header("시각 효과")]
    [SerializeField] GameObject _dustEffect;
    [SerializeField] float _dustRotationSpeed = 20f;
    [SerializeField] float _dustTargetScore = 100f;
    [SerializeField] float _toStarTargetScore = 300f;
    bool _isStar = false;

    [Header("행성스킨 SO")]
    [SerializeField] PlanetSkins _planetSkins;
    [SerializeField] PlanetSkins _starSkins;

    Vector2 _mousePos;
    Rigidbody2D _rb;
    SpriteRenderer _spr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            int myIndex = PlayerPrefs.GetInt("SelectedPlanetIndex", 0);
            photonView.RPC(nameof(RPC_ApplySkin), RpcTarget.AllBuffered, myIndex);

            if (UIManager.Instance != null) //매니저에게 세팅요청
            {
                UIManager.Instance.ConnectScore(_currentScore);
            }
        }
    }

    [PunRPC]
    private void RPC_ApplySkin(int index)
    {
        if (_spr != null && _planetSkins != null && index < _planetSkins.sprites.Length)
        {
            _spr.sprite = _planetSkins.sprites[index];
        }
    }

    private void Update()
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

    public void OnPointerInput(InputAction.CallbackContext ctx)
    {
        if (!photonView.IsMine) return;
        //마우스 스크린 좌표 받아오기
        Vector2 screenPos = ctx.ReadValue<Vector2>();
        _mousePos = Camera.main.ScreenToWorldPoint(screenPos);
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
    }

    //마우스 방향 바라보기
    private void HandleRotation()
    {
        Vector2 dir = (_mousePos - (Vector2)transform.position).normalized;
        if(dir != Vector2.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }
    }

    //이동
    private void HandleMovement(float speed)
    {
        float distance = Vector2.Distance(transform.position, _mousePos);

        //마우스가 플레이어에 가까우면 멈춤
        if(distance > 0.1f)
        {
            _rb.linearVelocity = transform.up * speed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
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

    public void OnHeadHitOhterPlayer()
    {
        if(!photonView.IsMine || _currentState == PlayerState.Dead) return;

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
}
