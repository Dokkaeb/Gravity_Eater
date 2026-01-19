using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance {  get; private set; }

    [SerializeField] SoundData _soundData;
    [SerializeField] AudioSource _bgmSource;
    [SerializeField] AudioSource _sfxSource;

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

        //¡ﬂ∫π ªÃ¿∏∏È ¥ŸΩ√ ªÃ±‚
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
}
