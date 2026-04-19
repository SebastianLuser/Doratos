using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/SpearSO")]
public class SpearSO : ScriptableObject
{
    public float minThrowSpeed = 8f;
    public float maxThrowSpeed = 25f;
    public float chargeMaxSec = 1.5f;
    public float damage = 100f;
    public float maxDistance = 15f;
    public float pickupRadius = 1.5f;
}
