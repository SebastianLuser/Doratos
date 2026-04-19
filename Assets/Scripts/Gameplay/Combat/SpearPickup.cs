using Photon.Pun;
using UnityEngine;

public class SpearPickup : MonoBehaviour
{
    private Spear spear;

    private void Awake()
    {
        spear = GetComponentInParent<Spear>();
        if (spear == null)
            spear = GetComponent<Spear>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spear == null || spear.State != SpearState.Grounded) return;

        var pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        spear.RequestPickup(pv.OwnerActorNr);
    }
}
