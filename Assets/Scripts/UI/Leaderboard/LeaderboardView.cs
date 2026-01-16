using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardView : MonoBehaviour
{
    [SerializeField] GameObject _rowPrefab;
    [SerializeField] GameObject _panel;
    [SerializeField] Transform _contentParent;

    private List<GameObject> _rows = new List<GameObject>();
    private LeaderboardPresenter _presenter;

    public void Setup(LeaderboardPresenter presenter) //UI매니저에서 호출할 메서드
    {
        _presenter = presenter;
    }

    public void TogglePanel(bool active)
    {
        _panel.SetActive(active);

        if (active && _presenter != null)
        {
            _presenter.RefreshGlobalScores();
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


}
