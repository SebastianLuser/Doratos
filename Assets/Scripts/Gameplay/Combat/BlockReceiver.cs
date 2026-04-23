using Photon.Pun;
using UnityEngine;

public class BlockReceiver : MonoBehaviourPun
{
    [Tooltip("Ventana de tolerancia en ms para compensar latencia de red.")]
    [SerializeField] private int toleranceMs = 200;

    private Shield shield;
    private int activatedTimestamp = int.MinValue;
    private bool isActive;

    private void Awake()
    {
        shield = GetComponentInChildren<Shield>();
    }
    
    public bool CanBlock(int hitTimestamp, Vector3 incomingDirection)
    {
        if (!isActive) return false;
        int delta = hitTimestamp - activatedTimestamp;
        if (delta < -toleranceMs) return false;

        return shield != null && shield.IsBlocking(incomingDirection);
    }

    
    [PunRPC]
    public void RPC_BlockActivated(int serverTimestamp)
    {
        activatedTimestamp = serverTimestamp;
        isActive = true;
        if (!photonView.IsMine && shield != null)
            shield.Activate();
    }

    [PunRPC]
    public void RPC_BlockDeactivated()
    {
        isActive = false;

        if (!photonView.IsMine && shield != null)
            shield.Deactivate();
    }
}
