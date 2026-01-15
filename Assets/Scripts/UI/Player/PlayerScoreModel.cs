using System;
using UnityEngine;

public class PlayerScoreModel
{
    float _score;
    public event Action<float> OnScoreChanged;

    public float Score
    {
        get => _score;
        set
        {
            if (Math.Abs(_score - value) > 0.01f)
            {
                _score = value;
                OnScoreChanged?.Invoke(_score);
            }
        }
    }
}
