using UnityEngine;
using System.Threading.Tasks;

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

    private async void Start()
    {
        //파이어 베이스 매니저 생성까지 잠시 대기
        while (FirebaseManager.Instance == null)
        {
            await Task.Yield();
        }
        Debug.Log("<color=yellow>[Lobby] Firebase 초기화 대기 중...</color>");
        //파이어베이스 내부 초기화 작업 끝날때까지 대기
        await FirebaseManager.Instance.WaitForInitialization;
        Debug.Log("<color=green>[Lobby] Firebase 준비 완료! 리더보드 연결 시작</color>");

        _leaderboardPresenter = new LeaderboardPresenter(_leaderboardView, FirebaseManager.Instance);
        _leaderboardView.Setup(_leaderboardPresenter);

        _leaderboardPresenter.RefreshGlobalScores();
    }
    public void OnClickLeaderboard()
    {
        _isLeaderboardOpen = !_isLeaderboardOpen;
        _leaderboardView.TogglePanel(_isLeaderboardOpen);
    }
}
