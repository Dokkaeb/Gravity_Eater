using UnityEngine;
using UnityEngine.EventSystems;


public class LeaderboardTrigger : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.ShowGlobalLeaderboard(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.ShowGlobalLeaderboard(false);
    }
    
}
