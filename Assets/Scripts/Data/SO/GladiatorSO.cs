using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/GladiatorSO")]
public class GladiatorSO : ScriptableObject
{
    public float maxHP = 100f;
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f;
    public float shieldMoveMultiplier = 0.5f;
    public float shieldMaxDuration = 2f;
    public float shieldCooldown = 3f;
    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 4f;
}
