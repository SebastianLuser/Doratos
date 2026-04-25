using UnityEngine;

public class Shield : MonoBehaviour
{
    [SerializeField] private GameObject shieldVisual;

    public bool IsActive { get; private set; }

    private void Start()
    {
        Deactivate();
    }

    public void Activate()
    {
        IsActive = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
    }

    public void Deactivate()
    {
        IsActive = false;
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    public bool IsBlocking(Vector3 incomingDirection)
    {
        if (!IsActive) return false;
        float dot = Vector3.Dot(transform.forward, -incomingDirection.normalized);
        return dot > 0.5f;
    }
}
