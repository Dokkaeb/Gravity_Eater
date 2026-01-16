using UnityEngine;
using UnityEngine.UI;

public class CustomSkin : MonoBehaviour
{
    [SerializeField] PlanetSkins _planets;
    Image _img;

    private void Awake()
    {
        _img = GetComponent<Image>();
    }

    public void Onselected(int index)
    {
        _img.sprite = _planets.sprites[index];
    }
}
