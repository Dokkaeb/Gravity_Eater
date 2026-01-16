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
        // 뷰에 프레젠터를 주입하여 뷰가 필요할 때 요청할 수 있게 함
        _leaderboardView.Setup(_leaderboardPresenter);

        // 시작 시 판넬은 꺼두기
        _leaderboardView.TogglePanel(false);
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
    public void ShowGlobalLeaderboard(bool show)
    {
        _leaderboardView.TogglePanel(show);
    }

    private void OnDestroy()
    {
        _scorePresenter?.Dispose();
    }
}
