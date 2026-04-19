using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/MatchConfigSO")]
public class MatchConfigSO : ScriptableObject
{
    public float endScreenDelaySec = 3f;
    public float spawnInvulnerableSec = 1.5f;
    public int maxPlayers = 4;
    public float fireRingDelaySec = 60f;
    public float fireRingShrinkSec = 30f;
    public float fireRingDPS = 25f;
}
