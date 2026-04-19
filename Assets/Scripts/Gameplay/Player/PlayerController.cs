using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviourPun
{
    [SerializeField] private GladiatorSO stats;

    private Rigidbody rb;
    private Camera mainCam;
    private PlayerState playerState;
    private Spear currentSpear;
    private bool hasSpear;

    private bool isCharging;
    private float chargeTimer;

    public float ChargeNormalized => currentSpear != null ? Mathf.Clamp01(chargeTimer / currentSpear.SpearData.chargeMaxSec) : 0f;
    public bool IsCharging => isCharging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();
        HideAimIndicator();
    }

    private void HideAimIndicator()
    {
        var aim = transform.Find("AimIndicator");
        if (aim != null)
        {
            var r = aim.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }
    }

    private void Start()
    {
        if (photonView.IsMine)
            mainCam = Camera.main;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (playerState.CurrentState != PlayerStateId.Default) return;

        if (!hasSpear || currentSpear == null) return;


        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        if (isCharging && Input.GetMouseButton(0))
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Min(chargeTimer, currentSpear.SpearData.chargeMaxSec);
        }

        if (isCharging && Input.GetMouseButtonUp(0))
        {
            TryThrowSpear();
            isCharging = false;
            chargeTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        HandleMovement();
        HandleRotation();
    }

    public void SetSpearReference(Spear spear)
    {
        currentSpear = spear;
    }

    private void TryThrowSpear()
    {
        if (currentSpear == null || currentSpear.State != SpearState.Held) return;
        if (currentSpear.HolderActorNr != photonView.OwnerActorNr) return;

        var data = currentSpear.SpearData;
        float t = Mathf.Clamp01(chargeTimer / data.chargeMaxSec);
        float speed = Mathf.Lerp(data.minThrowSpeed, data.maxThrowSpeed, t);

        Vector3 origin = transform.position + Vector3.up * 0.8f + transform.forward * 1f;
        currentSpear.RequestThrow(origin, transform.forward, speed);
        hasSpear = false;
    }

    public void OnSpearPickedUp(bool picked)
    {
        hasSpear = picked;
        isCharging = false;
        chargeTimer = 0f;
    }

    private void HandleMovement()
    {
        Vector3 direction;

        if (playerState != null && playerState.CurrentState == PlayerStateId.Dashing)
        {
            direction = playerState.DashDirection;
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            direction = new Vector3(h, 0f, v).normalized;
        }

        float speed = stats.moveSpeed;
        if (playerState != null)
            speed *= playerState.MoveSpeedMultiplier;

        Vector3 targetPos = rb.position + direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    private void HandleRotation()
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            Vector3 lookDir = point - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, stats.rotationSpeed * Time.fixedDeltaTime));
            }
        }
    }
}
