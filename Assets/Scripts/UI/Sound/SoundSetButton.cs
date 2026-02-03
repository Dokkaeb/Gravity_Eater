using UnityEngine;
using UnityEngine.EventSystems;

public class SoundSetButton : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.Instance.PlaySFX("sfx_Hover");
    }

    public void OnSelectSound()
    {
        SoundManager.Instance.PlaySFX("sfx_Select");
    }

    public void OnBackSound()
    {
        SoundManager.Instance.PlaySFX("sfx_Back");
    }
}
