
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance {  get; private set; }

    [Header("메인 UI")]
    [SerializeField] InputField _nickNameInput;
    [SerializeField] Button _joinButton;
    [SerializeField] Button _quitButton;
    [SerializeField] Text _statusText;

    [Header("리더보드 UI")]
    [SerializeField] LeaderboardView _leaderboardView;
    LeaderboardPresenter _leaderboardPresenter;
    bool _isLeaderboardOpen = false;

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_joinButton != null)
        {
            _joinButton.onClick.AddListener(OnClickStart);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnClickQuit);
        }
    }
    public void InitLeaderboard()
    {
        _leaderboardPresenter = new LeaderboardPresenter(_leaderboardView, FirebaseManager.Instance);
        _leaderboardView.Setup(_leaderboardPresenter);
        _leaderboardPresenter.RefreshGlobalScores();
    }

    public void UpdateStatus(string msg) => _statusText.text = msg;
    public void SetJoinButtonActive(bool isActive) => _joinButton.interactable = isActive;

    public void OnClickStart()
    {
        string name = _nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _joinButton.interactable = false;
        LobbyManager.Instance.Connect(name);
    }

    public void OnClickQuit()
    {
        if (GameExitManager.Instance != null)
        {
            GameExitManager.Instance.QuitGame();
        }
        else
        {
            // 만약 ExitManager가 없다면 직접 호출
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
    }

    public void OnClickLeaderboard()
    {
        _isLeaderboardOpen = !_isLeaderboardOpen;
        _leaderboardView.TogglePanel(_isLeaderboardOpen);
    }
}
