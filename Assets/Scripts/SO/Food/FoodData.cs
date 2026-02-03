using UnityEngine;

[CreateAssetMenu(fileName = "FoodData", menuName = "Scriptable Objects/FoodData")]
public class FoodData : ScriptableObject
{
    public string foodName;
    public float scoreAmount;
    public float scaleSize;
    public Sprite foodSprite;
}
