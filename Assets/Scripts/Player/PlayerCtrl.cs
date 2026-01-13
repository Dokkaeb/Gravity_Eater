using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerCtrl : MonoBehaviourPun
{
    public enum PlayerState { Move, Dash ,Dead }

    [Header("기본세팅")]
    [SerializeField] PlayerState _currentState = PlayerState.Move;
    [SerializeField] float _moveSpeed = 5f;

    [Header("성장 세팅")]
    [SerializeField] float _currentScore = 0;
    [SerializeField] float _scaleMultiplier = 0.1f; //점수당 크기증가 비율
    [SerializeField] float _minScale = 1.0f;

    Vector2 _mousePos;
    Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        switch( _currentState)
        {
            case PlayerState.Move:
                HandleMovement();
                break;
            case PlayerState.Dead:
                break;
            case PlayerState.Dash:
                break;
        }
    }

    public void OnPointerInput(InputAction.CallbackContext ctx)
    {
        if (!photonView.IsMine) return;
        //마우스 스크린 좌표 받아오기
        Vector2 screenPos = ctx.ReadValue<Vector2>();
        _mousePos = Camera.main.ScreenToWorldPoint(screenPos);

        if(_currentState != PlayerState.Dead)
        {
            _currentState = PlayerState.Move;
        }
    }
    public void ChangeState(PlayerState newState)
    {
        _currentState = newState;
    }

    private void HandleMovement()
    {
        //마우스 방향으로 이동
        Vector2 dir = (_mousePos - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, _mousePos);

        //마우스가 플레이어에 가까우면 멈춤
        if(distance > 0.1f)
        {
            _rb.linearVelocity = dir * _moveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public void AddScore(float scoreAmount)
    {
        if (!photonView.IsMine) return;

        _currentScore += scoreAmount;

        UpdateScale();

        photonView.RPC(nameof(RPC_UpdateScore), RpcTarget.Others, _currentScore);
    }
    [PunRPC]
    private void RPC_UpdateScore(float newScore)
    {
        _currentScore = newScore;
        UpdateScale();
    }

    private void UpdateScale()
    {
        float newScale = _minScale + (_currentScore * _scaleMultiplier);
        transform.localScale = new Vector3(newScale, newScale, 1f);


    }
}
