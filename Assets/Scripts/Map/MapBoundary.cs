using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] float _mapSize = 110f;
    [SerializeField] float _detectDistance = 15f;
    [SerializeField] Color _lineColor = Color.cyan;
    [SerializeField] float _lineWidth = 0.8f;

    [Header("워프 설정")]
    [SerializeField] float _warpOffset = 10f;

    LineRenderer _lineRenderer;
    Transform _playerTransform;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }
    private void Start()
    {    
        SetupBoundary();
    }

    private void SetupBoundary()
    {
        float s = _mapSize;
        Vector3[] corners = {
            new Vector3(-s,-s,0),
            new Vector3(s,-s,0),
            new Vector3(s,s,0),
            new Vector3(-s,s,0)
        };

        _lineRenderer.positionCount = corners.Length;
        _lineRenderer.SetPositions(corners);
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.loop = true;
    }

    private void Update()
    {
        if (PlayerCtrl.LocalPlayer == null) return;
        _playerTransform = PlayerCtrl.LocalPlayer.transform;

        UpdateBoundaryAlpha();
    }

    private void UpdateBoundaryAlpha()
    {
        // 플레이어와 맵 경계 사이의 최단 거리 계산
        float distToEdge = GetDistanceToEdge(_playerTransform.position);

        // 거리에 따른 알파값 계산 (0~1 사이)
        // 가까울수록 1(불투명), 멀수록 0(투명)
        float alpha = 1.0f - Mathf.Clamp01(distToEdge / _detectDistance);

        // 컬러 적용
        Color targetColor = _lineColor;
        targetColor.a = alpha;

        _lineRenderer.startColor = targetColor;
        _lineRenderer.endColor = targetColor;
    }

    //플레이어 기준 최단거리 반환
    private float GetDistanceToEdge(Vector3 pos)
    {
        // 맵 안쪽에서의 거리 계산
        float distToRight = _mapSize - pos.x;
        float distToLeft = _mapSize + pos.x;
        float distToTop = _mapSize - pos.y;
        float distToBottom = _mapSize + pos.y;

        // 네 방향 중 가장 가까운 벽과의 거리를 선택
        float minDist = Mathf.Min(distToRight, distToLeft, distToTop, distToBottom);

        return minDist;
    }

    public void BoundaryWarp(string wallName)
    {
        Vector3 pos = _playerTransform.position;
        bool isWarped = false;

        // 플레이어 크기에 따른 마진 확보
        float playerRadius = _playerTransform.localScale.x * 0.8f;
        float finalOffset = _warpOffset + playerRadius;

        switch (wallName)
        {
            case "Right":
                pos.x = -_mapSize + finalOffset;
                isWarped = true;
                break;
            case "Left":
                pos.x = _mapSize - finalOffset;
                isWarped = true;
                break;
            case "Top":
                pos.y = -_mapSize + finalOffset;
                isWarped = true;
                break;
            case "Bottom":
                pos.y = _mapSize - finalOffset;
                isWarped = true;
                break;
        }

        if (isWarped)
        {
            _playerTransform.position = pos;

            if (CamFollow.Instance != null)
            {
                CamFollow.Instance.FastMove(pos);
            }

            Rigidbody2D rb = _playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Debug.Log("강제 워프");
        }
    }
}
