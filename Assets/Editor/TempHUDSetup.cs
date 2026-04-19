using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class TempHUDSetup
{
    [MenuItem("Tools/Setup HUD Health Bars")]
    public static void SetupHUD()
    {
        var hud = Object.FindAnyObjectByType<HUD>();
        if (hud == null)
        {
            Debug.LogError("No se encontró componente HUD en la escena.");
            return;
        }

        var hudGO = hud.gameObject;
        Canvas canvas = hudGO.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = hudGO.GetComponent<Canvas>();
        if (canvas == null)
        {
            var canvasGO = GameObject.Find("HUDCanvas");
            if (canvasGO != null) canvas = canvasGO.GetComponent<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogError("No se encontró Canvas.");
            return;
        }

        Transform canvasTransform = canvas.transform;

        // Remove old health bar objects if they exist
        string[] oldNames = { "OwnHealthBar", "EnemyHealthBar1", "EnemyHealthBar2", "EnemyHealthBar3" };
        foreach (var n in oldNames)
        {
            var old = canvasTransform.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // Remove old corner containers
        for (int i = 0; i < 4; i++)
        {
            var old = canvasTransform.Find("HealthBar_P" + (i + 1));
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        Color[] colors = new Color[]
        {
            new Color(0.2f, 0.6f, 1f),    // Blue - P1
            new Color(1f, 0.3f, 0.3f),    // Red - P2
            new Color(0.3f, 1f, 0.3f),    // Green - P3
            new Color(1f, 0.85f, 0.2f)    // Yellow - P4
        };

        // Corners: top-left, top-right, bottom-left, bottom-right
        Vector2[] anchorsMin = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        Vector2[] anchorsMax = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        Vector2[] pivots = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        Vector2[] positions = { new Vector2(15, -15), new Vector2(-15, -15), new Vector2(15, 15), new Vector2(-15, 15) };

        float barWidth = 140f;
        float barHeight = 14f;

        Image[] barImages = new Image[4];
        TextMeshProUGUI[] labelTexts = new TextMeshProUGUI[4];

        for (int i = 0; i < 4; i++)
        {
            // Container
            var container = new GameObject("HealthBar_P" + (i + 1), typeof(RectTransform));
            container.transform.SetParent(canvasTransform, false);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = anchorsMin[i];
            containerRect.anchorMax = anchorsMax[i];
            containerRect.pivot = pivots[i];
            containerRect.anchoredPosition = positions[i];
            containerRect.sizeDelta = new Vector2(barWidth, barHeight + 20f);

            // Background bar
            var bgGO = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(container.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.pivot = new Vector2(0.5f, 1);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0, barHeight);
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Fill bar
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGO.GetComponent<Image>();
            fillImage.color = colors[i];
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            barImages[i] = fillImage;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(container.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, barHeight > 20 ? -2 : 0);
            labelRect.sizeDelta = new Vector2(0, 16f);
            var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
            labelTMP.text = "Player " + (i + 1);
            labelTMP.fontSize = 11f;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = colors[i];

            labelTexts[i] = labelTMP;
        }

        // Assign to HUD via SerializedObject
        var so = new SerializedObject(hud);
        var barsProp = so.FindProperty("healthBars");
        var labelsProp = so.FindProperty("playerLabels");

        barsProp.arraySize = 4;
        labelsProp.arraySize = 4;

        for (int i = 0; i < 4; i++)
        {
            barsProp.GetArrayElementAtIndex(i).objectReferenceValue = barImages[i];
            labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = labelTexts[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);

        Debug.Log("HUD Health Bars configuradas en las 4 esquinas.");
    }
}
