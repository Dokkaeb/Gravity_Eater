using System;
using System.Collections.Generic;
using UnityEngine;

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
            // FirebaseManager가 준비될 때까지 안전하게 기다린 후 호출
            if (FirebaseManager.Instance != null)
            {
                List<ScoreEntry> topScores = await FirebaseManager.Instance.GetTopScoresAsync(10);
                _view.UpdateLeaderboard(topScores);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"리더보드 갱신 대기 중: {e.Message}");
        }
    }
}
