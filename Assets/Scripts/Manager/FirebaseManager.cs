using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    DatabaseReference _dbRef;
    string _userID;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if(dependencyStatus == DependencyStatus.Available)
            {
                //초기화 성공시 참조 설정
                _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                //익명 로그인 사용하지 않는 경우 간단히 deviceID로 구분
                _userID = SystemInfo.deviceUniqueIdentifier;
                Debug.Log("파이어베이스 초기화 완료");
            }
            else
            {
                Debug.LogError($"Firebase 의존성 문제: {dependencyStatus}");
            }
        });
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
        List<ScoreEntry> scores = new List<ScoreEntry>();

        if(_dbRef==null) return scores;

        //점수기준 내림차순 정렬해서 제한된 수만큼 가져오기
        DataSnapshot snapshot = await _dbRef.Child("leaderboard")
            .OrderByChild("score")
            .LimitToLast(limit)
            .GetValueAsync();

        if (snapshot.Exists)
        {
            foreach(var child in snapshot.Children)
            {
                string nick = child.Child("nickname").Value.ToString();
                float sc = float.Parse(child.Child("score").Value.ToString());
                scores.Add(new ScoreEntry(nick, sc));
            }
            scores.Reverse();
        }
        return scores;
    }
}
