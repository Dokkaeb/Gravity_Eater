using UnityEngine;

public class LobyUIManager : MonoBehaviour
{
    public static LobyUIManager Instance {  get; private set; }

    [Header("리더보드 UI")]
    [SerializeField] LeaderboardView _leaderboardView;
    LeaderboardPresenter _leaderboardPresenter;
    bool _isLeaderboardOpen = false;

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _leaderboardPresenter = new LeaderboardPresenter(_leaderboardView, FirebaseManager.Instance);
        _leaderboardView.Setup(_leaderboardPresenter);
    }
    public void OnClickLeaderboard()
    {
        _isLeaderboardOpen = !_isLeaderboardOpen;
        _leaderboardView.TogglePanel(_isLeaderboardOpen);
    }
}
