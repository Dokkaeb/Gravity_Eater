using UnityEditor.Rendering;
using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    LineRenderer _lineRenderer;
    Transform _playerTransform;

    [Header("설정")]
    [SerializeField] float _mapSize = 100f;
    [SerializeField] float _detectDistance = 15f;
    [SerializeField] Color _lineColor = Color.cyan;
    [SerializeField] float _lineWidth = 0.8f;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupLine();
    }

    private void SetupLine()
    {
        _lineRenderer.positionCount = 4;
        _lineRenderer.loop = true;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;

        float s = _mapSize;
        Vector3[] corners = { 
            new Vector3(-s,-s,0),
            new Vector3(s,-s,0),
            new Vector3(s,s,0),
            new Vector3(-s,s,0)
        };
        _lineRenderer.SetPositions(corners);

        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        Color c = _lineColor;
        c.a = 0;
        _lineRenderer.startColor = c;
        _lineRenderer.endColor = c;
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            if (PlayerCtrl.LocalPlayer != null)
            {
                _playerTransform = PlayerCtrl.LocalPlayer.transform;
            }
            return;
        }

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

        // 만약 맵 밖으로 나간 경우(dx, dy > 0)까지 고려한 안전장치
        if (Mathf.Abs(pos.x) > _mapSize || Mathf.Abs(pos.y) > _mapSize)
            return 0;

        return minDist;
    }
}
