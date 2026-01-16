using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LeaderboardView : MonoBehaviour
{
    [SerializeField] GameObject _rowPrefab;
    [SerializeField] GameObject _panel;
    [SerializeField] Transform _contentParent;

    [Header("펼쳐지는 효과")]
    [SerializeField] float _animationSpeed = 5f;

    private List<GameObject> _rows = new List<GameObject>();
    private LeaderboardPresenter _presenter;
    private Coroutine _currentAnim;

    public void Setup(LeaderboardPresenter presenter) //UI매니저에서 호출할 메서드
    {
        _presenter = presenter;
        _panel.transform.localScale = new Vector3(1, 0, 1);
        _panel.SetActive(false);
    }

    public void TogglePanel(bool active)
    {
        if (_currentAnim != null) StopCoroutine(_currentAnim);

        if (active)
        {
            _panel.SetActive(true);
            _currentAnim = StartCoroutine(Co_AnimatePanel(1f)); // 위에서 아래로 펼치기

            if (_presenter != null)
                _presenter.RefreshGlobalScores();
        }
        else
        {
            _currentAnim = StartCoroutine(Co_AnimatePanel(0f)); // 아래에서 위로 접기
        }
    }

    public void UpdateLeaderboard(List<ScoreEntry> entries)
    {
        foreach (var row in _rows) Destroy(row);
        _rows.Clear();

        if (entries == null || entries.Count == 0)
        {
            // 데이터가 없을 때 표시할 임시 로직
            Debug.Log("불러올 점수가 없습니다.");
            return;
        }

        for (int i =0; i< entries.Count; i++)
        {
            GameObject row = Instantiate(_rowPrefab, _contentParent);
            row.GetComponent<TextMeshProUGUI>().text = $"{i + 1}. {entries[i].Nickname} - {entries[i].Score:F0}";
            _rows.Add(row);
        }
    }

    private IEnumerator Co_AnimatePanel(float targetY)
    {
        Vector3 initialScale = _panel.transform.localScale;
        Vector3 targetScale = new Vector3(1, targetY, 1);
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * _animationSpeed;
            // Lerp를 이용해 부드럽게 스케일 변화
            _panel.transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }

        _panel.transform.localScale = targetScale;

        // 완전히 접혔으면 오브젝트 비활성화
        if (targetY <= 0f)
        {
            _panel.SetActive(false);
        }
    }
}
