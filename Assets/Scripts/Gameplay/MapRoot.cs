using UnityEngine;

public class MapRoot : MonoBehaviour
{
    public static MapRoot Current { get; private set; }
    private void Awake()     => Current = this;
    private void OnDestroy() { if (Current == this) Current = null; }
}
