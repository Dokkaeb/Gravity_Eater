using TMPro;
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

    [Header("사망 UI")]
    [SerializeField] GameObject _deathPanel;
    [SerializeField] TextMeshProUGUI _finalScoreTxt;

    [Header("기타 UI")]
    [SerializeField] GameObject _setPanel;
    [SerializeField] GameObject _soundSetPanel;

    [Header("파이어 베이스 매니저")]
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

    public void ShowDeathUI(float finalScore)
    {
        if (_setPanel != null && _soundSetPanel != null) 
        { 
            _soundSetPanel.SetActive(false);
            _setPanel.SetActive(false); 
        }

        if(_deathPanel != null)
        {
            _deathPanel.SetActive(true);
            _finalScoreTxt.text = $"Final Score: {finalScore:F0}";
        }
        ShowGlobalLeaderboard(true);
    }

    public void OnClickExitButton()
    {
        // 현재 점수를 Presenter에서 가져옴
        float score = _scorePresenter != null ? _scorePresenter.GetCurrentScore() : 0;

        // 전담 매니저에게 나가는 프로세스 위임
        GameExitManager.Instance.ExitToMain(score);
    }
}
