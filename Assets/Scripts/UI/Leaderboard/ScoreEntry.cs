using UnityEngine;

public struct ScoreEntry
{
    public string Nickname;
    public float Score;

    public ScoreEntry(string nickname,float score)
    {
        Nickname = nickname;
        Score = score;
    }
}
