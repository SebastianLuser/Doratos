using System.Collections;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class SpikeTrap : MonoBehaviourPun
{
    [Header("Animation")]  
    [SerializeField] private Animator spikeTrapAnim;

    [Header("Settings")]
    [SerializeField] private float effectDelaySeconds = 0.4f, cooldownSeconds = 5f, damage = 15f;

    [Header("Slow Effect")]
    [SerializeField] private float slowMultiplier = 0.5f, slowDurationSeconds = 2f;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private Vector3 detectionCenter = Vector3.zero;
    [SerializeField] private Vector3 detectionHalfExtents = new Vector3(0.5f, 0.3f, 0.5f);

    private bool isActive = true, isOpen = false;
    private Vector3 WorldCenter => transform.TransformPoint(detectionCenter);

    private void Awake() => spikeTrapAnim = GetComponent<Animator>();

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || !isActive || isOpen) return;
        int actorNr = GetActorInTrap();
        if (actorNr >= 0) photonView.RPC(nameof(RPC_ActivateTrap), RpcTarget.All, actorNr);
    }

    private int GetActorInTrap()
    {
        foreach (var hit in Physics.OverlapBox(WorldCenter, detectionHalfExtents, transform.rotation, playerLayerMask, QueryTriggerInteraction.Ignore))
        {
            var pv = hit.GetComponentInParent<PhotonView>() ?? hit.GetComponentInChildren<PhotonView>();
            if (pv != null) return pv.OwnerActorNr;
        }
        return -1;
    }

    [PunRPC]
    private void RPC_ActivateTrap(int actorNr)
    {
        isOpen = true; isActive = false;
        spikeTrapAnim.SetTrigger("open");
        StartCoroutine(TrapSequence(actorNr));
    }

    private IEnumerator TrapSequence(int actorNr)
    {
        yield return new WaitForSeconds(effectDelaySeconds);

        foreach (var hit in Physics.OverlapBox(WorldCenter, detectionHalfExtents, transform.rotation, playerLayerMask, QueryTriggerInteraction.Ignore))
        {
            var pv = hit.GetComponent<PhotonView>();
            if (pv == null || pv.OwnerActorNr != actorNr) continue;

            // Authoritative: MasterClient only
            if (PhotonNetwork.IsMasterClient)
            {
                pv.gameObject.GetComponent<PlayerHealth>()
                    ?.photonView.RPC(nameof(PlayerHealth.RPC_TakeDamage), RpcTarget.All, damage);

                var spear = Spear.Current;
                if (spear != null && spear.State == SpearState.Held && spear.HolderActorNr == actorNr)
                    spear.photonView.RPC(nameof(Spear.RPC_SpearGrounded), RpcTarget.All, spear.transform.position);
            }

            // Local: owning client only
            if (pv.IsMine)
                pv.gameObject.GetComponent<SlowEffect>()?.ApplySlow(slowMultiplier, slowDurationSeconds);

            break;
        }

        yield return new WaitForSeconds(.5f);
        spikeTrapAnim.SetTrigger("close");
        isOpen = false;

        yield return new WaitForSeconds(cooldownSeconds);
        isActive = true;
    }
}