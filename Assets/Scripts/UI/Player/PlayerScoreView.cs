using UnityEngine;
using TMPro;
using DG.Tweening;

public class PlayerScoreView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _scoreTxt;
    Tween _scoreTween;

    public void UpdateScoreDisPlay(float score)
    {
        _scoreTxt.text = $"Score : {score:F2}";
        _scoreTween?.Kill(true);
        _scoreTween = _scoreTxt.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
    }
    private void OnDestroy()
    {
        _scoreTween?.Kill();
    }
}
