using UnityEngine;

public class FireRing : MonoBehaviour
{
    public float CurrentRadius { get; private set; }
    public bool IsActive { get; private set; }

    private float startRadius;
    private float shrinkDuration;
    private float elapsedTime;

    public void Activate(float radius, float duration)
    {
        startRadius = radius;
        shrinkDuration = duration;
        CurrentRadius = radius;
        elapsedTime = 0f;
        IsActive = true;
        gameObject.SetActive(true);
        UpdateScale();
    }

    private void Update()
    {
        if (!IsActive) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / shrinkDuration);
        CurrentRadius = Mathf.Lerp(startRadius, 0f, t);
        UpdateScale();
    }

    private void UpdateScale()
    {
        float diameter = CurrentRadius * 2f;
        transform.localScale = new Vector3(diameter, 0.01f, diameter);
    }
}
