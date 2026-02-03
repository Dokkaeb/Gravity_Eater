using UnityEngine;

public class Top10Txt : MonoBehaviour
{
    bool _isAct = false;

    public void OnEnableTxt()
    {
        _isAct = !_isAct;
        gameObject.SetActive(_isAct);
    }
}
