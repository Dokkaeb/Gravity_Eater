using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("스코어 UI")]
    [SerializeField] PlayerScoreView _scoreView;
    PlayerScorePresenter _scorePresenter;

    [Header("리더보드 UI")]
    [SerializeField] LeaderboardView _leaderboardView;
    LeaderboardPresenter _leaderboardPresenter;

    [SerializeField] private FirebaseManager _firebaseManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _leaderboardPresenter = new LeaderboardPresenter(_leaderboardView, _firebaseManager);
        _leaderboardPresenter.RefreshGlobalScores();
    }

    //MVP연결
    public void ConnectScore(float score)
    {
        if (_scoreView == null) return;

        PlayerScoreModel scoreModel = new PlayerScoreModel();

        _scorePresenter = new PlayerScorePresenter(scoreModel,_scoreView);

        _scorePresenter.SetScore(score);
    }
    //점수 업데이트 요청 전달
    public void UpdatePlayerScore(float newScore)
    {
        _scorePresenter?.SetScore(newScore);
    }

    //사망시 또는 로비에서 호출
    public void ShowGlobalLeaderboard()
    {
        _leaderboardPresenter.RefreshGlobalScores();
        _leaderboardView.TogglePanel(true);
    }

    private void OnDestroy()
    {
        _scorePresenter?.Dispose();
    }
}
