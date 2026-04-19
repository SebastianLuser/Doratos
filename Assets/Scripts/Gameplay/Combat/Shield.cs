using UnityEngine;

public class Shield : MonoBehaviour
{
    [SerializeField] private GameObject shieldVisual;

    private static readonly Color shieldColor = new Color(0.45f, 0.25f, 0.1f);

    public bool IsActive { get; private set; }

    private void Start()
    {
        ApplyColor();
        Deactivate();
    }

    private void ApplyColor()
    {
        if (shieldVisual == null) return;
        var r = shieldVisual.GetComponent<Renderer>();
        if (r != null) r.material.color = shieldColor;
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
