using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class LeaderboardPresenter
{
    readonly LeaderboardView _view;
    readonly FirebaseManager _firebase;

    public LeaderboardPresenter(LeaderboardView view, FirebaseManager firebase)
    {
        _view = view;
        _firebase = firebase;
    }

    public async void RefreshGlobalScores()
    {
        try
        {
            if (FirebaseManager.Instance == null) return;

            // 초기화될 때까지 최대 5초만 기다림
            var initTask = FirebaseManager.Instance.WaitForInitialization;
            if (await Task.WhenAny(initTask, Task.Delay(5000)) == initTask)
            {
                // 초기화 성공 혹은 완료됨
                List<ScoreEntry> topScores = await FirebaseManager.Instance.GetTopScoresAsync(10);

                if (topScores != null)
                {
                    _view.UpdateLeaderboard(topScores);
                }
            }
            else
            {
                Debug.LogWarning("Firebase 초기화 지연 중... 다음 시도에 다시 확인합니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"리더보드 갱신 중 오류 : {e.Message}");
        }
    }
}
