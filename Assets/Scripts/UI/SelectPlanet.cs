using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SelectPlanet : MonoBehaviour
{
    private int _currentIndex = 0;

    public void UpdateCurrentIndex(int index)
    {
        _currentIndex = index;
    }

    public void OnConfirmSelection()
    {
        // 1. 로컬 저장 (게임을 껐다 켜도 유지되도록)
        PlayerPrefs.SetInt("SelectedPlanetIndex", _currentIndex);

        // 2. Photon 네트워크에 내 속성 등록 (다른 유저들이 볼 수 있게)
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Hashtable props = new Hashtable
            {
                { "PlanetIndex", _currentIndex }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        Debug.Log($"행성 선택 확정: {_currentIndex}번");
    }
}
