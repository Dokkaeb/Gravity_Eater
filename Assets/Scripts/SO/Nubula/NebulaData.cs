using UnityEngine;

[CreateAssetMenu(fileName = "NebulaData", menuName = "Scriptable Objects/NebulaData")]
public class NebulaData : ScriptableObject
{
    public string nebulaName;
    public Nebula.NebulaType type;
    public Sprite sprite;

    [Header("¼³Á¤°ª")]
    public float minDuration = 5f;
    public float maxDuration = 15f;
    public float scorePenalty = 10f;
    public float slowAmount = 0.5f;
}
