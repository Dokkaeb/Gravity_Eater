using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("스코어 UI")]
    [SerializeField] PlayerScoreView _scoreView;
    
    PlayerScorePresenter _scorePresenter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

    private void OnDestroy()
    {
        _scorePresenter?.Dispose();
    }
}
