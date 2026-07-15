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
    public float cueSmoothSpeed = 18.0f;

    [Header("Default Design")]
    public string currentDesign = "outline";

    [Header("Cue GameObjects")]
    public GameObject overlayCue;
    public GameObject outlineCue;
    public Transform innerTransform;
    public Transform outerTransform;

    [Header("Loss of Tracking Settings")]
    public bool cueVisible = false;
    public float visibilityDecayDelay = 1.0f;
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
            if (_decayTimer < 0f)
            {
                SetVisibility(false);
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
        SetAlpha("0.9");
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
        _decayTimer = visibilityDecayDelay;
        SetVisibility(true);

        currentSize = new Vector2(scale.x, scale.y);

        if (outlineCue)
        {
            if (innerTransform != null)
            {
                innerTransform.position = Vector3.Lerp(innerTransform.position, position, Time.deltaTime * cueSmoothSpeed);
                innerTransform.rotation = rotation;
                innerTransform.localScale = new Vector3(currentSize.x, currentSize.y, 1f);
            }

            if (outerTransform != null)
            {
                outerTransform.position = Vector3.Lerp(outerTransform.position, position, Time.deltaTime * cueSmoothSpeed); // Sync position lock
                outerTransform.rotation = rotation;

                // Total visual size = core tracked dimensions + thickness on both sides
                float totalWidth = currentSize.x + (currentThickness * 2f);
                float totalHeight = currentSize.y + (currentThickness * 2f);
                outerTransform.localScale = new Vector3(totalWidth, totalHeight, 1f);
            }
        }

        if (overlayCue)
        {
            overlayCue.transform.position = Vector3.Lerp(overlayCue.transform.position, position, Time.deltaTime * cueSmoothSpeed);
            overlayCue.transform.rotation = rotation;
            overlayCue.transform.localScale = scale;
        }
    }

    public void SetVisibility(bool visible)
    {
        cueVisible = visible;

        // Only currently enabled cue is shown
        if (outlineCue) {
            outlineCue.SetActive(visible && currentDesign == "outline");
        }
        if (overlayCue)
        {
            overlayCue.SetActive(visible && currentDesign == "overlay");
        }
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
}