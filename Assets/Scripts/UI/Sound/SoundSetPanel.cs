using UnityEngine;
using UnityEngine.UI;

public class SoundSetPanel : MonoBehaviour
{
    [Header("BGM 슬라이더")]
    [SerializeField] Slider _bgmSlider;
    [Header("효과음 슬라이더")]
    [SerializeField] Slider _sfxSlider;

    private void OnEnable()
    {
        if(SoundManager.Instance != null)
        {
            _bgmSlider.value = SoundManager.Instance.GetBGMVolume();
            _sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        }

        _bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        _sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
    }

    private void OnDisable()
    {
        _bgmSlider.onValueChanged.RemoveAllListeners();
        _sfxSlider.onValueChanged.RemoveAllListeners();
    }
}
