using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CanvasGroupExtensions
{
    public static void SetVisibility(this CanvasGroup canvasGroup, bool enabled)
    {
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
        canvasGroup.alpha = enabled ? 1f : 0f;
    }
}
