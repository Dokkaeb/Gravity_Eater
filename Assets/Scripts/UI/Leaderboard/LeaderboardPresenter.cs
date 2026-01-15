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
        // Firebase에서 Top 10 데이터를 비동기로 가져옴
        List<ScoreEntry> topScores = await _firebase.GetTopScoresAsync(10);
        _view.UpdateLeaderboard(topScores);
    }
}
