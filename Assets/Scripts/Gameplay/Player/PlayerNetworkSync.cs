using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool networkIsShielding;
    private byte networkState;

    private PlayerState playerState;
    private Shield shield;

    public bool NetworkIsShielding => networkIsShielding;
    public PlayerStateId NetworkState => (PlayerStateId)networkState;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        shield = GetComponentInChildren<Shield>();
    }

    private void Update()
    {
        if (photonView.IsMine) return;

        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * lerpSpeed);

        if (shield != null)
        {
            if (networkIsShielding) shield.Activate();
            else shield.Deactivate();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(shield != null && shield.IsActive);
            stream.SendNext((byte)(playerState != null ? playerState.CurrentState : PlayerStateId.Default));
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsShielding = (bool)stream.ReceiveNext();
            networkState = (byte)stream.ReceiveNext();
        }
    }
}
