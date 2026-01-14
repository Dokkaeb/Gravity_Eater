using UnityEngine;

public class PlayerScorePresenter
{
    readonly PlayerScoreModel _model;
    readonly PlayerScoreView _view;

    public PlayerScorePresenter(PlayerScoreModel model, PlayerScoreView view)
    {
        _model = model;
        _view = view;

        _model.OnScoreChanged += _view.UpdateScoreDisPlay;
    }

    public void SetScore(float newScore)
    {
        _model.Score = newScore;
    }

    public void Dispose()
    {
        _model.OnScoreChanged -= _view.UpdateScoreDisPlay;
    }
}
