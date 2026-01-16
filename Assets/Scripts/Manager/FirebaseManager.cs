using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    DatabaseReference _dbRef;
    string _userID;

    // 초기화 완료 여부를 외부에서 기다릴 수 있도록 TaskCompletionSource 사용
    private TaskCompletionSource<bool> _initializationTask = new TaskCompletionSource<bool>();
    public Task WaitForInitialization => _initializationTask.Task;

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeFirebaseAsync();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task InitializeFirebaseAsync()
    {
        try
        {
            // 의존성 체크를 await로 처리 (ContinueWith 대신)
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                
                // 로컬 기기에 저장된 ID가 없다면 새로 생성
                if (!PlayerPrefs.HasKey("TestUserID"))
                {
                    // GUID를 사용하여 절대 겹치지 않는 고유 문자열 생성
                    string newID = "User_" + Guid.NewGuid().ToString().Substring(0, 8);
                    PlayerPrefs.SetString("TestUserID", newID);
                }

                // 저장된 ID를 불러와서 사용
                _userID = PlayerPrefs.GetString("TestUserID");
                

                Debug.Log("<color=green><b>[Firebase]</b> 초기화 성공!</color>");

                _initializationTask.TrySetResult(true);
            }
            else
            {
                Debug.LogError($"[Firebase] 의존성 문제: {dependencyStatus}");
                _initializationTask.TrySetResult(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] 초기화 중 예외 발생: {e.Message}");
            _initializationTask.TrySetException(e);
        }
    }

    //사망시 호출되는 점수기록
    public async Task UpdateHighScore(string nickname,float score)
    {

        if(_dbRef == null) return;

        //최신점수 업데이트
        var data = new Dictionary<string, object>
        {
            {"nickname",nickname },
            {"score",score }
        };

        await _dbRef.Child("leaderboard").Child(_userID).SetValueAsync(data);
        Debug.Log("Firebase 점수 업데이트 성공");
    }

    //상위 10명 데이터 가져오기
    public async Task<List<ScoreEntry>> GetTopScoresAsync(int limit = 10)
    {
        // 초기화가 완료될 때까지 대기
        await WaitForInitialization;

        List<ScoreEntry> scores = new List<ScoreEntry>();

        if (_dbRef == null)
        {
            Debug.LogError("파이어베이스 데이터 레퍼런스가없음");
            return scores;
        }

        try
        {
            // .GetValueAsync() 대신 .OrderByChild().LimitToLast() 사용 시 
            // 서버 규칙에 반드시 indexOn이 있어야 합니다.
            DataSnapshot snapshot = await _dbRef.Child("leaderboard")
                .OrderByChild("score")
                .LimitToLast(limit)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    string nick = child.Child("nickname").Value?.ToString() ?? "Unknown";
                    float sc = float.Parse(child.Child("score").Value?.ToString() ?? "0");
                    scores.Add(new ScoreEntry(nick, sc));
                }
                scores.Reverse();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase Query Error: {e.Message}");
        }
        return scores;
    }
}
