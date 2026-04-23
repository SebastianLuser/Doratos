using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviourPun, IPunObservable
{
    // Posición y rotación las maneja PhotonTransformView (componente del profesor).
    // Este componente solo sincroniza datos de gameplay que PhotonTransformView no cubre.

    private byte networkState;
    private PlayerState playerState;

    public PlayerStateId NetworkState => (PlayerStateId)networkState;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)(playerState != null ? playerState.CurrentState : PlayerStateId.Default));
        }
        else
        {
            networkState = (byte)stream.ReceiveNext();
        }
    }
}
