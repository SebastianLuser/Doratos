using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/SlotColors")]
public class PlayerColorsSO : ScriptableObject
{
    public Color[] SlotColors = new Color[4]
    {
        new Color(0.2f, 0.6f, 1f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.3f, 1f, 0.3f),
        new Color(1f, 0.85f, 0.2f)
    };
}
