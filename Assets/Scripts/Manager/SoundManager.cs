using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance {  get; private set; }

    [SerializeField] SoundData _soundData;
    [SerializeField] AudioSource _bgmSource;
    [SerializeField] AudioSource _sfxSource;

    //볼륨 설정 저장할 키값
    const string BGM_VOLUME_KEY = "BGM_Volume";
    const string SFX_VOLUME_KEY = "SFX_Volume";

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        float bgmVol = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.5f);



        PlayRandomBGM();
    }
    private void Update()
    {
        if (!_bgmSource.isPlaying)
        {
            PlayRandomBGM();
        }
    }
    public void PlayRandomBGM()
    {
        if (_soundData.bgmClips.Length == 0) return;

        int randomIndex = Random.Range(0,_soundData.bgmClips.Length);

        //중복 뽑으면 다시 뽑기
        if (_bgmSource.clip == _soundData.bgmClips[randomIndex].clip) { PlayRandomBGM(); return; }

        _bgmSource.clip = _soundData.bgmClips[randomIndex].clip;
        _bgmSource.loop = false;
        _bgmSource.Play();
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = _soundData.GetSFX(sfxName);
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }

    public void SetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
    }
    public void SetSFXVolume(float volume)
    {
        _sfxSource.volume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
    }

    //슬라이더 초기화용 현재 설정된 볼륨값 가져오는 메서드
    public float GetBGMVolume() => PlayerPrefs.GetFloat(BGM_VOLUME_KEY,0.5f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFX_VOLUME_KEY,0.5f);
}
