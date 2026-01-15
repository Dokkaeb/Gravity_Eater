using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardView : MonoBehaviour
{
    [SerializeField] GameObject _rowPrefab;
    [SerializeField] GameObject _panel;
    [SerializeField] Transform _contentParent;

    private List<GameObject> _rows = new List<GameObject>();

    public void TogglePanel(bool active)
    {
        _panel.SetActive(active);
    }

    public void UpdateLeaderboard(List<ScoreEntry> entries)
    {
        foreach (var row in _rows) Destroy(row);

        _rows.Clear();

        for(int i =0; i< entries.Count; i++)
        {
            GameObject row = Instantiate(_rowPrefab, _contentParent);
            row.GetComponent<TextMeshProUGUI>().text = $"{i + 1}. {entries[i].Nickname} - {entries[i].Score:F0}";
            _rows.Add(row);
        }
    }
}
