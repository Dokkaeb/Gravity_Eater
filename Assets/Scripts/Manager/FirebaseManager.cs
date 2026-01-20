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

                GenerateNewUserID();

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
    // ID를 새로 발급
    public void GenerateNewUserID()
    {
        _userID = "User_" + Guid.NewGuid().ToString().Substring(0, 8);
    }

    //사망시 호출되는 점수기록
    public async Task UpdateHighScore(string nickname, float score)
    {

        if (_dbRef == null) return;

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
        await WaitForInitialization;
        List<ScoreEntry> scores = new List<ScoreEntry>();

        if (_dbRef == null) return scores;

        // 리더보드 노드를 실시간 동기화 (연결성 개선)
        _dbRef.Child("leaderboard").KeepSynced(true);

        int maxRetries = 5; // 재접속 시도 횟수
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // 쿼리 작업 생성
                var queryTask = _dbRef.Child("leaderboard")
                    .OrderByChild("score")
                    .LimitToLast(limit)
                    .GetValueAsync();

                // 타임아웃 설정 (3초 안에 응답 없으면 취소로 간주)
                var timeoutTask = Task.Delay(3000);
                var completedTask = await Task.WhenAny(queryTask, timeoutTask);

                if (completedTask == queryTask)
                {
                    DataSnapshot snapshot = await queryTask;

                    if (snapshot.Exists && snapshot.ChildrenCount > 0)
                    {
                        foreach (var child in snapshot.Children)
                        {
                            string nick = child.Child("nickname").Value?.ToString() ?? "Unknown";
                            float sc = Convert.ToSingle(child.Child("score").Value ?? 0);
                            scores.Add(new ScoreEntry(nick, sc));
                        }
                        scores.Reverse();
                        return scores;
                    }
                }

                Debug.LogWarning($"[Firebase] {i + 1}번째 시도 실패 (타임아웃 또는 데이터 없음). 재시도 중...");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] 시도 {i} 중 예외: {e.Message}");
            }

            await Task.Delay(1000); // 재시도 간격을 1초로 늘려 서버 세션 안정화 유도
        }

        return scores;
    }
}
