using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Va en la RAÍZ del Gladiador (mismo GameObject que el PhotonView).
/// Photon no busca RPCs en hijos — todos los MonoBehaviourPun con RPCs
/// deben estar en el mismo GameObject que el PhotonView.
///
/// swordAnimator y swordCollider referencian el hijo "Sword" via Inspector.
/// </summary>
public class MeleeWeapon : MonoBehaviourPun
{
    [SerializeField] private MeleeSO data;
    [SerializeField] private Animator  swordAnimator;    // Animator en el hijo "Sword"
    [SerializeField] private Collider  swordCollider;    // BoxCollider trigger en el hijo "Sword"

    private static readonly int SwingTrigger = Animator.StringToHash("Swing");

    private float cooldownTimer;
    private bool isSwinging;

    // Evita golpear al mismo enemigo más de una vez por swing
    private readonly HashSet<int> hitThisSwing = new HashSet<int>();

    private void Awake()
    {
        if (swordCollider != null)
            swordCollider.isTrigger = true;

        DisableCollider();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // ── API pública ──────────────────────────────────────────────────────────

    public bool IsReady => cooldownTimer <= 0f && !isSwinging;

    /// <summary>
    /// Llamado por PlayerController cuando el jugador presiona la tecla de ataque.
    /// PlayerController ya verifica: estado Default y sin lanza.
    /// </summary>
    public void RequestSwing()
    {
        if (!IsReady) return;

        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_Swing), RpcTarget.All, photonView.OwnerActorNr);
        else
            photonView.RPC(nameof(RPC_RequestSwingToMC), RpcTarget.MasterClient, photonView.OwnerActorNr);
    }

    // ── RPCs ─────────────────────────────────────────────────────────────────

    [PunRPC]
    private void RPC_RequestSwingToMC(int actorNr)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Validación básica: el actor existe en la sala
        if (PhotonNetwork.CurrentRoom.GetPlayer(actorNr) == null) return;

        photonView.RPC(nameof(RPC_Swing), RpcTarget.All, actorNr);
    }

    [PunRPC]
    private void RPC_Swing(int attackerActorNr)
    {
        isSwinging = true;
        hitThisSwing.Clear();
        EnableCollider();
        StartCoroutine(SwingRoutine());
    }

    // ── Colisión (solo Master Client) ────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!isSwinging) return;

        // --- Jugadores reales ---
        var health = other.GetComponent<PlayerHealth>();
        if (health != null && !health.IsDead)
        {
            int targetActor = health.photonView.OwnerActorNr;
            if (targetActor == photonView.OwnerActorNr) return;  // no golpear al propio
            if (hitThisSwing.Contains(targetActor)) return;       // ya fue golpeado este swing
            hitThisSwing.Add(targetActor);

            Vector3 incomingDir = (other.transform.position - transform.position).normalized;
            var blockReceiver   = other.GetComponent<BlockReceiver>();
            bool isBlocked      = blockReceiver != null &&
                                  blockReceiver.CanBlock(PhotonNetwork.ServerTimestamp, incomingDir);

            if (isBlocked)
            {
                // Bloqueado: ralentizar al ATACANTE
                photonView.RPC(nameof(SlowEffect.RPC_ApplySlow), RpcTarget.All,
                               data.slowMultiplier, data.slowDuration);

                if (HUD.Instance != null)
                    HUD.Instance.ShowFeedback("¡Golpe bloqueado!");
            }
            else
            {
                // No bloqueado: ralentizar al DEFENSOR
                health.photonView.RPC(nameof(SlowEffect.RPC_ApplySlow), RpcTarget.All,
                                      data.slowMultiplier, data.slowDuration);

                // Anti-stall: si el defensor tiene la lanza, se la transferimos al atacante
                if (Spear.Current != null &&
                    Spear.Current.State == SpearState.Held &&
                    Spear.Current.HolderActorNr == targetActor)
                {
                    Spear.Current.RequestTransfer(photonView.OwnerActorNr);
                }
            }
            return;
        }

    }

    // ── Animación por Animator ───────────────────────────────────────────────

    private IEnumerator SwingRoutine()
    {
        // Dispará la animación visual en todos los clientes (ya estamos dentro de RPC_Swing)
        if (swordAnimator != null)
            swordAnimator.SetTrigger(SwingTrigger);

        // El collider permanece activo durante swingDuration (igual que antes)
        yield return new WaitForSeconds(data.swingDuration);

        DisableCollider();
        isSwinging    = false;
        cooldownTimer = data.cooldown;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnableCollider()
    {
        if (swordCollider != null) swordCollider.enabled = true;
    }

    private void DisableCollider()
    {
        if (swordCollider != null) swordCollider.enabled = false;
    }
}
