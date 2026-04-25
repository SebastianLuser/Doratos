using Photon.Pun;
using UnityEngine;

/// <summary>
/// Componente reutilizable de slow. Va en la raíz del Gladiador.
/// Cualquier fuente de daño (melee, lanza a futuro, etc.) puede aplicar slow
/// llamando RPC_ApplySlow via el PhotonView del jugador afectado.
/// PlayerController lee Multiplier al calcular la velocidad.
/// </summary>
public class SlowEffect : MonoBehaviourPun
{
    public float Multiplier { get; private set; } = 1f;

    private float slowTimer;

    private void Update()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                Multiplier = 1f;
        }
    }

    [PunRPC]
    public void RPC_ApplySlow(float multiplier, float duration)
    {
        Multiplier  = multiplier;
        slowTimer   = duration;

        if (HUD.Instance != null && photonView.IsMine)
            HUD.Instance.ShowFeedback("¡Estás ralentizado!", duration);
    }
}
