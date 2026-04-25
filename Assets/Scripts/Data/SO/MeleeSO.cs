using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/MeleeSO")]
public class MeleeSO : ScriptableObject
{
    [Header("Swing")]
    public float swingDuration   = 0.4f;   // segundos que dura la animación / ventana de hit
    public float cooldown        = 1.5f;   // cooldown después de completar el swing
    public float swingStartAngle = -70f;   // ángulo inicial de la espada (local Y)
    public float swingEndAngle   =  70f;   // ángulo final de la espada (local Y)

    [Header("Slow")]
    public float slowMultiplier  = 0.4f;   // velocidad al recibir slow (40% de la normal)
    public float slowDuration    = 2f;     // segundos que dura el slow
}
