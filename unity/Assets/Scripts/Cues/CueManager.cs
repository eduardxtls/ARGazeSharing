using UnityEngine;

[ExecuteAlways]
public class CueManager : MonoBehaviour
{
    [Header("Editor Simulation Settings")]
    public bool simulateInEditor = false;
    public Vector3 simulatedScale = new Vector3(1f, 1f, 1f);
    [Range(0.01f, 1.0f)]
    public float simulatedThickness = 0.1f; // Set directly in world meters (e.g., 0.05 = 5cm)

    [Header("Movement Smoothness Settings")]
    public float outlineSmooth = 0.05f;
    public float overlaySmooth = 0.05f;

    [Header("Distance-Based Fading Settings")]
    [Tooltip("Distance at which the cue starts to fade out.")]
    public float fadeOutDistance = 0.1f;
    [Tooltip("Distance at which the cue starts to fade out.")]
    public float fadeInDistance = 0.1f;

    private float _currentMaxAlpha = 1.0f;

    [Header("Default Design")]
    public string currentDesign = "outline";

    [Header("Cue GameObjects")]
    public GameObject overlayCue;
    public GameObject outlineCue;
    public Transform innerTransform;
    public Transform outerTransform;

    [Header("Loss of Tracking Settings")]
    public float visibilityDecayDelay = 0.5f;
    private float _decayTimer = 0f;

    private Renderer overlayRenderer;
    private SpriteRenderer outerSpriteRenderer;

    private Vector3 outlineVelocity = Vector3.zero;
    private Vector3 overlayVelocity = Vector3.zero;

    // Outline size settings
    private float currentThickness = 0.01f;
    private Vector2 currentSize = new Vector2(1f, 1f);


    private void OnValidate()
    {
        CacheMaskingReferences();
        ExecuteLayoutUpdates();
    }

    private void Update()
    {
        // Force the editor viewport to redraw continuously while dragging sliders
        if (!Application.isPlaying && simulateInEditor)
        {
            ExecuteLayoutUpdates();
        }

        if (_decayTimer > 0f)
        {
            _decayTimer -= Time.deltaTime;

            if (_decayTimer <= 0f)
            {
                if (outlineCue) outlineCue.SetActive(false);
                if (overlayCue) overlayCue.SetActive(false);
            }
        }
    }



    private void Awake()
    {
        if (overlayCue) overlayRenderer = overlayCue.GetComponent<Renderer>();

        // Get the references for the outline cue
        CacheMaskingReferences();

        // Default initialization
        SetDesign(currentDesign);
        SetColor("#32cd32");
        SetAlpha("0.25");
        SetVisibility(true);
    }

    public void CacheMaskingReferences()
    {
        if (outerTransform != null && outerSpriteRenderer == null)
        {
            outerSpriteRenderer = outerTransform.GetComponent<SpriteRenderer>();
        }

        if (overlayCue != null && overlayRenderer == null)
        {
            overlayRenderer = overlayCue.GetComponent<Renderer>();
        }
    }

    private void ExecuteLayoutUpdates()
    {
        if (simulateInEditor)
        {
            // CHANGE 1: Driving the script thickness variable from the slider value
            currentThickness = simulatedThickness;
            UpdateCueTransform(transform.position, transform.rotation, simulatedScale);

#if UNITY_EDITOR
            // Instantly repaints the scene view to bypass Unity's editor rendering freeze
            UnityEditor.SceneView.RepaintAll();
#endif
        }
    }


    public void ApplyDesign(DesignMessage design)
    {
        string value = design.value;
        switch (design.parameter.ToLower())
        {
            case "cue_type":    SetDesign(value);   break;
            case "size":        SetSize(value);     break;
            case "color":       SetColor(value);    break;
            case "alpha":       SetAlpha(value);    break;
        }
    }

    public void UpdateCueTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        currentSize = new Vector2(scale.x, scale.y);

        Vector3 currentPos = innerTransform != null ? innerTransform.position : Vector3.zero;
        float distanceToTarget = Vector3.Distance(currentPos, position);

        float alphaPercentage = Mathf.InverseLerp(fadeOutDistance, fadeInDistance, distanceToTarget);
        SetAlphaValue(alphaPercentage * _currentMaxAlpha);

        if (outlineCue)
        {
            if (innerTransform != null)
            {
                innerTransform.position = Application.isPlaying ? Vector3.SmoothDamp(innerTransform.position, position, ref outlineVelocity, outlineSmooth) : position;
                innerTransform.rotation = rotation;
                innerTransform.localScale = new Vector3(currentSize.x, currentSize.y, 1f);
            }

            if (outerTransform != null)
            {
                outerTransform.position = Application.isPlaying ? innerTransform.position : position; // Sync position lock
                outerTransform.rotation = rotation;

                // Total visual size = core tracked dimensions + thickness on both sides
                float totalWidth = currentSize.x + (currentThickness * 2f);
                float totalHeight = currentSize.y + (currentThickness * 2f);
                outerTransform.localScale = new Vector3(totalWidth, totalHeight, 1f);
            }
        }

        if (overlayCue)
        {
            overlayCue.transform.position = Vector3.SmoothDamp(overlayCue.transform.position, position, ref overlayVelocity, overlaySmooth);
            overlayCue.transform.rotation = rotation;
            overlayCue.transform.localScale = scale;
        }
    }

    public void SetVisibility(bool visible)
    {
        if (visible)
        {
            _decayTimer = visibilityDecayDelay;

            if (outlineCue)
            {
                outlineCue.SetActive(visible && currentDesign == "outline");
            }
            if (overlayCue)
            {
                overlayCue.SetActive(visible && currentDesign == "overlay");
            }
        }

        /*
        // Only currently enabled cue is shown
        if (outlineCue) {
            outlineCue.SetActive(visible && currentDesign == "outline");
        }
        if (overlayCue)
        {
            overlayCue.SetActive(visible && currentDesign == "overlay");
        }
        */
    }

    // Setters for design parameters
    public void SetDesign(string design)
    {
        currentDesign = design.ToLower();
        
        outlineCue.SetActive(currentDesign == "outline");
        overlayCue.SetActive(currentDesign == "overlay");

        Debug.Log($"Design Change - Design: {design}");
    }

    public void SetSize(string size)
    {
        switch (size.ToLower())
        {
            case "tiny":    currentThickness = 0.005f; break;
            case "small":   currentThickness = 0.01f; break;
            case "medium":  currentThickness = 0.025f; break;
            case "large":   currentThickness = 0.05f; break;
            case "huge":    currentThickness = 0.075f; break;
            default:        Debug.Log($"Unknown size value: {size}"); return;
        }

        Debug.Log($"Design Change - Size: {size}");
    }

    public void SetColor(string color)
    {
        if (UnityEngine.ColorUtility.TryParseHtmlString(color, out Color newColor))
        {
            newColor.a = outerSpriteRenderer.color.a; // Preserve current alpha - same for both cues
            _currentMaxAlpha = newColor.a;
            outerSpriteRenderer.color = newColor;
            overlayRenderer.sharedMaterial.color = newColor;
            Debug.Log("Design Change - Color: " + color);
        }
        else
        {
            Debug.Log($"Invalid color value: {color}");
        }   
    }

    public void SetAlpha(string alpha)
    {
        if (float.TryParse(alpha, out float alphaValue))
        {
            _currentMaxAlpha = alphaValue;
            SetAlphaValue(alphaValue);

            if (outerSpriteRenderer != null)
            {
                Color color = outerSpriteRenderer.color;
                color.a = alphaValue;
                outerSpriteRenderer.color = color;
            }

            if (overlayRenderer != null)
            {
                Color color = overlayRenderer.sharedMaterial.color;
                color.a = alphaValue;
                overlayRenderer.sharedMaterial.color = color;
            }
            
            Debug.Log($"Design Change - Alpha: {alpha}");
        }
        else
        {
            Debug.Log($"Invalid alpha value: {alpha}");
        }
    }

    public void SetAlphaValue(float alphaValue)
    {
        if (outerSpriteRenderer != null)
        {
            Color color = outerSpriteRenderer.color;
            color.a = alphaValue;
            outerSpriteRenderer.color = color;
        }
        if (overlayRenderer != null)
        {
            Color color = overlayRenderer.sharedMaterial.color;
            color.a = alphaValue;
            overlayRenderer.sharedMaterial.color = color;
        }
    }
}