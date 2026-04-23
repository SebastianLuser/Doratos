using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public enum SpearState : byte
{
    Held,
    InFlight,
    Grounded
}

public class Spear : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private SpearSO spearData;

    public SpearSO SpearData => spearData;
    public SpearState State { get; private set; } = SpearState.Grounded;
    public int HolderActorNr { get; private set; } = -1;

    private Rigidbody rb;
    private Collider col;
    private Vector3 throwOrigin;
    private float maxDistanceSqr;

    private static Dictionary<int, PlayerHealth> playersByActorNr = new Dictionary<int, PlayerHealth>();

    private static readonly Color shaftColor = new Color(0.45f, 0.25f, 0.1f);
    private static readonly Color tipColor = new Color(0.6f, 0.6f, 0.6f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        ApplyColors();
    }

    private void Start()
    {
        if (HUD.Instance != null)
            HUD.Instance.RegisterSpear(this);
    }

    private void ApplyColors()
    {
        var shaft = transform.Find("Shaft");
        if (shaft != null)
        {
            var r = shaft.GetComponent<Renderer>();
            if (r != null) r.material.color = shaftColor;
        }

        var tip = transform.Find("Tip");
        if (tip != null)
        {
            var r = tip.GetComponent<Renderer>();
            if (r != null) r.material.color = tipColor;
        }
    }

    public static void RegisterPlayer(int actorNr, PlayerHealth health)
    {
        playersByActorNr[actorNr] = health;
    }

    public static void UnregisterPlayer(int actorNr)
    {
        playersByActorNr.Remove(actorNr);
    }

    public static void ClearPlayers()
    {
        playersByActorNr.Clear();
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (State == SpearState.InFlight)
        {
            Vector3 offset = transform.position - throwOrigin;
            if (offset.sqrMagnitude >= maxDistanceSqr)
                photonView.RPC(nameof(RPC_SpearGrounded), RpcTarget.All, transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (State != SpearState.InFlight) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null && !health.IsDead)
        {
            int hitActorNr = health.photonView.OwnerActorNr;
            if (hitActorNr == HolderActorNr) return;

            var shield = other.GetComponentInChildren<Shield>();
            bool isShielding = shield != null && shield.IsActive;

            if (isShielding && shield.IsBlocking(rb.velocity.normalized))
            {
                photonView.RPC(nameof(RPC_SpearGrounded), RpcTarget.All, transform.position);
            }
            else
            {
                health.photonView.RPC(nameof(PlayerHealth.RPC_TakeDamage), RpcTarget.All, spearData.damage);
                photonView.RPC(nameof(RPC_SpearGrounded), RpcTarget.All, transform.position);
            }
            return;
        }

    }

    [PunRPC]
    public void RPC_Throw(Vector3 origin, Vector3 direction, int throwerActorNr, float speed)
    {
        State = SpearState.InFlight;
        HolderActorNr = throwerActorNr;
        transform.SetParent(null);
        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(direction);

        rb.isKinematic = false;
        rb.velocity = direction.normalized * speed;
        col.isTrigger = true;

        throwOrigin = origin;
        maxDistanceSqr = spearData.maxDistance * spearData.maxDistance;
    }

    [PunRPC]
    public void RPC_SpearGrounded(Vector3 position)
    {
        State = SpearState.Grounded;
        HolderActorNr = -1;
        transform.SetParent(null);
        transform.position = position;

        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }
        col.isTrigger = true;
    }

    [PunRPC]
    public void RPC_PickedUp(int actorNr)
    {
        State = SpearState.Held;
        HolderActorNr = actorNr;

        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (playersByActorNr.TryGetValue(actorNr, out PlayerHealth holder))
        {
            Transform socket = holder.transform.Find("SpearSocket");
            if (socket != null)
            {
                transform.SetParent(socket);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            var controller = holder.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetSpearReference(this);
                controller.OnSpearPickedUp(true);
            }
        }
    }

    public void RequestThrow(Vector3 origin, Vector3 direction, float speed)
    {
        if (State != SpearState.Held) return;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_Throw), RpcTarget.All, origin, direction, HolderActorNr, speed);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestThrowToMC), RpcTarget.MasterClient, origin, direction, PhotonNetwork.LocalPlayer.ActorNumber, speed);
        }
    }

    [PunRPC]
    private void RPC_RequestThrowToMC(Vector3 origin, Vector3 direction, int actorNr, float speed)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (State != SpearState.Held || HolderActorNr != actorNr) return;

        photonView.RPC(nameof(RPC_Throw), RpcTarget.All, origin, direction, actorNr, speed);
    }

    public void RequestPickup(int actorNr)
    {
        photonView.RPC(nameof(RPC_RequestPickupToMC), RpcTarget.MasterClient, actorNr);
    }

    [PunRPC]
    private void RPC_RequestPickupToMC(int actorNr)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (State != SpearState.Grounded) return;

        photonView.RPC(nameof(RPC_PickedUp), RpcTarget.All, actorNr);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)State);
            stream.SendNext(HolderActorNr);
        }
        else
        {
            State = (SpearState)(byte)stream.ReceiveNext();
            HolderActorNr = (int)stream.ReceiveNext();
        }
    }
}
