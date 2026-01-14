using UnityEngine;
using TMPro;

public class PlayerScoreView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _scoreTxt;

    public void UpdateScoreDisPlay(float score)
    {
        _scoreTxt.text = $"Score : {score:F2}";
    }
}
