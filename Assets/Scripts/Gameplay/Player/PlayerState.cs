using Photon.Pun;
using UnityEngine;

public enum PlayerStateId : byte
{
    Default,
    Shielding,
    Dashing
}

public class PlayerState : MonoBehaviourPun
{
    [SerializeField] private GladiatorSO stats;

    public PlayerStateId CurrentState { get; private set; } = PlayerStateId.Default;
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public bool IsInvulnerable { get; private set; }
    public float ShieldCooldownNormalized => stats.shieldCooldown > 0 ? shieldCooldownTimer / stats.shieldCooldown : 0f;

    private float shieldCooldownTimer;
    private float shieldActiveTimer;
    private float dashCooldownTimer;
    private float dashActiveTimer;
    private Vector3 dashDirection;

    private Shield shield;
    private BlockReceiver blockReceiver;

    public Vector3 DashDirection => dashDirection;

    private void Awake()
    {
        shield = GetComponentInChildren<Shield>();
        blockReceiver = GetComponent<BlockReceiver>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (shieldCooldownTimer > 0f)
            shieldCooldownTimer -= Time.deltaTime;
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case PlayerStateId.Default:
                MoveSpeedMultiplier = 1f;
                IsInvulnerable = false;

                if (Input.GetMouseButton(1) && shieldCooldownTimer <= 0f)
                    EnterShielding();
                else if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0f)
                    EnterDashing();
                break;

            case PlayerStateId.Shielding:
                MoveSpeedMultiplier = stats.shieldMoveMultiplier;
                IsInvulnerable = false;

                shieldActiveTimer -= Time.deltaTime;
                if (!Input.GetMouseButton(1) || shieldActiveTimer <= 0f)
                {
                    shieldCooldownTimer = stats.shieldCooldown;
                    ExitShield();
                }
                break;

            case PlayerStateId.Dashing:
                dashActiveTimer -= Time.deltaTime;
                if (dashActiveTimer <= 0f)
                {
                    dashCooldownTimer = stats.dashCooldown;
                    ExitToDefault();
                }
                break;
        }
    }

    private void EnterShielding()
    {
        CurrentState = PlayerStateId.Shielding;
        shieldActiveTimer = stats.shieldMaxDuration;
        if (shield != null) shield.Activate();

        // Broadcast con el ServerTimestamp del momento exacto de activación.
        // BlockReceiver.CanBlock() usará este timestamp para validar bloqueos.
        photonView.RPC(nameof(BlockReceiver.RPC_BlockActivated), RpcTarget.All,
                       PhotonNetwork.ServerTimestamp);
    }

    // Salida específica del estado Shielding: notifica a BlockReceiver antes de resetear.
    private void ExitShield()
    {
        photonView.RPC(nameof(BlockReceiver.RPC_BlockDeactivated), RpcTarget.All);
        ExitToDefault();
    }

    private void EnterDashing()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        dashDirection = new Vector3(h, 0f, v).normalized;
        if (dashDirection.sqrMagnitude < 0.01f)
            dashDirection = transform.forward;

        CurrentState = PlayerStateId.Dashing;
        dashActiveTimer = stats.dashDuration;
        MoveSpeedMultiplier = stats.dashSpeedMultiplier;
        IsInvulnerable = false;
    }

    public void ResetToDefault()
    {
        // Si el jugador muere mientras bloquea, notificar a BlockReceiver
        if (CurrentState == PlayerStateId.Shielding)
            photonView.RPC(nameof(BlockReceiver.RPC_BlockDeactivated), RpcTarget.All);
        ExitToDefault();
    }

    private void ExitToDefault()
    {
        CurrentState = PlayerStateId.Default;
        MoveSpeedMultiplier = 1f;
        IsInvulnerable = false;
        if (shield != null) shield.Deactivate();
    }
}
