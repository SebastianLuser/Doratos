using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
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

    public static Spear Current { get; private set; }

    public SpearSO SpearData => spearData;
    public SpearState State { get; private set; } = SpearState.Grounded;
    public int HolderActorNr { get; private set; } = -1;

    private Rigidbody rb;
    private Collider col;
    private Vector3 throwOrigin;
    private float maxDistanceSqr;

    private static Dictionary<int, PlayerHealth> playersByActorNr = new Dictionary<int, PlayerHealth>();

    private void Awake()
    {
        Current = this;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Start()
    {
        if (HUD.Instance != null)
            HUD.Instance.RegisterSpear(this);
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
            // Distancia máxima de vuelo
            Vector3 offset = transform.position - throwOrigin;
            if (offset.sqrMagnitude >= maxDistanceSqr)
            {
                StopSpearFlight();
                return;
            }

            // Si el aro de fuego está activo y la lanza lo abandona → grounded al instante
            if (MatchManager.Instance != null && MatchManager.Instance.FireRing != null &&
                MatchManager.Instance.FireRing.IsActive)
            {
                float distFromCenter = new Vector2(transform.position.x, transform.position.z).magnitude;
                if (distFromCenter > MatchManager.Instance.FireRing.CurrentRadius)
                    StopSpearFlight();
            }
        }
    }

    private void StopSpearFlight()
    {
       if (State == SpearState.InFlight)
            photonView.RPC(nameof(RPC_SpearGrounded), RpcTarget.All, transform.position);
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

        if (other.CompareTag("Obstacle"))
            StopSpearFlight();
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
        // Notificar al dueño anterior que perdió la lanza
        if (HolderActorNr != -1 && HolderActorNr != actorNr)
        {
            if (playersByActorNr.TryGetValue(HolderActorNr, out PlayerHealth prevHolder))
            {
                var prevController = prevHolder.GetComponent<PlayerController>();
                if (prevController != null)
                    prevController.OnSpearPickedUp(false);
            }
        }

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

    /// <summary>
    /// Transferencia forzada desde Master Client (ej: golpe melee).
    /// No requiere que la lanza esté Grounded — funciona estando Held.
    /// </summary>
    public void RequestTransfer(int newHolderActorNr)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (State != SpearState.Held) return;

        photonView.RPC(nameof(RPC_PickedUp), RpcTarget.All, newHolderActorNr);
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
