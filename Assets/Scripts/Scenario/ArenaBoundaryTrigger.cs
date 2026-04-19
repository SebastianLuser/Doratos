using Photon.Pun;
using UnityEngine;

public class ArenaBoundaryTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var spear = other.GetComponent<Spear>();
        if (spear != null && spear.State == SpearState.InFlight)
        {
            spear.photonView.RPC(nameof(Spear.RPC_SpearGrounded), RpcTarget.All, Vector3.zero);
        }
    }
}
