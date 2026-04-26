using System.Collections;
using Photon.Pun;
using UnityEngine;

public class SlowEffect : MonoBehaviourPun
{
    public float Multiplier { get; private set; } = 1f;

    private Coroutine slowCoroutine;

    public void ApplySlow(float multiplier, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    [PunRPC]
    public void RPC_ApplySlow(float multiplier, float duration)
    {
        ApplySlow(multiplier, duration);
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        Debug.Log("Slow Effect Applied!");
        Multiplier = multiplier;
        yield return new WaitForSeconds(duration);
        Multiplier = 1f;
    }
}