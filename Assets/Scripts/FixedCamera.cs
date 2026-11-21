using UnityEngine;

/// <summary>
/// Fixed (non-following) orthographic camera that shows exactly <c>referenceWidth x referenceHeight</c> pixels
/// of the world at <c>pixelsPerUnit</c>: 352 x 256 px at 16 PPU = 22 x 16 tiles. When the window has a different
/// aspect ratio the view is letterboxed/pillarboxed so the picture is never stretched or cropped.
/// Put it on the scene's Main Camera. The camera never moves; change <c>center</c> to frame a level.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class FixedCamera : MonoBehaviour
{
    [Tooltip("Target view width in pixels.")]
    [SerializeField, Min(1)] private int referenceWidth = 352;

    [Tooltip("Target view height in pixels.")]
    [SerializeField, Min(1)] private int referenceHeight = 256;

    [Tooltip("Sprite pixels per world unit (1 tile = 1 unit at 16 PPU).")]
    [SerializeField, Min(1f)] private float pixelsPerUnit = 16f;

    [Tooltip("World position the camera is fixed at (centre of the view). Z is kept from the transform.")]
    [SerializeField] private Vector2 center = Vector2.zero;

    [Tooltip("Colour of the bars shown when the window aspect ratio differs from the target.")]
    [SerializeField] private Color barColor = Color.black;

    private Camera cam;
    private Camera barCamera;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        CreateBarCamera();
        Apply();
    }

    // Window can be resized at runtime; only touch the viewport when its size actually changed.
    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
            Apply();
    }

    private void OnValidate()
    {
        // Keeps the Scene/Game view in sync with inspector edits while not playing.
        if (cam == null)
            cam = GetComponent<Camera>();
        if (cam != null)
            Apply();
    }

    private void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        cam.orthographic = true;
        cam.orthographicSize = referenceHeight / (2f * pixelsPerUnit);

        Vector3 p = transform.position;
        transform.position = new Vector3(center.x, center.y, p.z);

        float targetAspect = (float)referenceWidth / referenceHeight;
        float windowAspect = lastHeight > 0 ? (float)lastWidth / lastHeight : targetAspect;

        Rect rect;
        if (windowAspect > targetAspect)
        {
            float w = targetAspect / windowAspect; // window too wide: bars left and right
            rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        else
        {
            float h = windowAspect / targetAspect; // window too tall: bars top and bottom
            rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }

        // The viewport is only changed while playing: setting it in edit mode (OnValidate) wrote editor-window-dependent
        // values into the Main Camera prefab and scenes. Edit mode keeps the saved (full-screen) rect.
        if (Application.isPlaying)
            cam.rect = rect;

        if (barCamera != null)
            barCamera.backgroundColor = barColor;
    }

    // A viewport smaller than the screen leaves the rest of the frame uncleared, so a second camera
    // that renders nothing but a solid colour sits behind the main one and fills the bars.
    private void CreateBarCamera()
    {
        GameObject go = new GameObject("LetterboxBars");
        go.transform.SetParent(transform, false);

        barCamera = go.AddComponent<Camera>();
        barCamera.clearFlags = CameraClearFlags.SolidColor;
        barCamera.backgroundColor = barColor;
        barCamera.cullingMask = 0;
        barCamera.orthographic = true;
        barCamera.depth = cam.depth - 1f;
        barCamera.allowHDR = false;
        barCamera.allowMSAA = false;
    }
}
