using UnityEngine;

[System.Serializable]
public struct SoundClip
{
    public string name;
    public AudioClip clip;
}

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("BGM 리스트")]
    public SoundClip[] bgmClips;

    [Header("SFX 리스트")]
    public SoundClip[] sfxClips;

    //이름으로 BGM찾는 기능
    public AudioClip GetBGM(string clipName)
    {
        foreach (var bgm in bgmClips)
        {
            if (bgm.name == clipName) return bgm.clip;
        }
        return null;
    }

    // 이름으로 SFX를 찾는 기능
    public AudioClip GetSFX(string clipName)
    {
        foreach (var sfx in sfxClips)
        {
            if (sfx.name == clipName) return sfx.clip;
        }
        return null;
    }
}
