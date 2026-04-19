using Photon.Pun;
using UnityEngine;

public class PlayerHealth : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private GladiatorSO stats;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => stats != null ? stats.maxHP : 100f;
    public bool IsDead { get; private set; }
    public float HealthNormalized => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

    private void Start()
    {
        CurrentHealth = MaxHealth;

        if (!photonView.IsMine && MatchManager.Instance != null)
            MatchManager.Instance.RegisterRemotePlayer(this, photonView.OwnerActorNr);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (IsDead) return;

        var state = GetComponent<PlayerState>();
        if (state != null && state.IsInvulnerable) return;

        CurrentHealth -= damage;

        if (HUD.Instance != null && photonView.IsMine)
            HUD.Instance.ShowFeedback("¡Recibiste daño!");

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        DisableComponents();

        if (MatchManager.Instance != null)
            MatchManager.Instance.NotifyPlayerDied(photonView.OwnerActorNr);
    }

    private void DisableComponents()
    {
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var state = GetComponent<PlayerState>();
        if (state != null)
        {
            state.ResetToDefault();
            state.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (photonView != null)
            Spear.UnregisterPlayer(photonView.OwnerActorNr);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(CurrentHealth);
            stream.SendNext(IsDead);
        }
        else
        {
            CurrentHealth = (float)stream.ReceiveNext();
            IsDead = (bool)stream.ReceiveNext();
        }
    }
}
