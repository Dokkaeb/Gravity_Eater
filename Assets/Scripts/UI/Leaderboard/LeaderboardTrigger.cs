using UnityEngine;
using UnityEngine.EventSystems;


public class LeaderboardTrigger : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.ShowGlobalLeaderboard(true);
        SoundManager.Instance.PlaySFX("sfx_Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.ShowGlobalLeaderboard(false);
        SoundManager.Instance.PlaySFX("sfx_Back");
    }
    
}
