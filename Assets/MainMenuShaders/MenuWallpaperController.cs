using UnityEngine;

/// <summary>
/// Drives the MainMenu/* interactive wallpaper shaders.
///
/// Setup (camera-facing Quad — recommended so your FullscreenDither
/// post-pass dithers the wallpaper too):
///   1. Create a Quad (GameObject > 3D Object > Quad), make it a child of
///      the menu Camera, and assign one of the MainMenu/* materials to it.
///   2. Put this component on the Camera (or any menu object).
///   3. Assign:  Cam = menu camera,  Quad = the quad's Transform,
///      WallpaperRenderer = the quad's MeshRenderer,
///      Wallpapers = the 5 materials (Hyperspace, Molten, Grid, Shards, Flux).
///   4. Press Play. Move the mouse / click to interact. Number keys 1-5
///      (and arrow keys) switch wallpapers; remove that block if unwanted.
///
/// The Quad is auto-fitted to fill the camera every frame, so its size and
/// placement don't matter. Uses the legacy Input Manager.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Transform))]
public class MenuWallpaperController : MonoBehaviour
{
    [Header("Targets")]
    public Camera cam;
    public Transform quad;
    public MeshRenderer wallpaperRenderer;

    [Tooltip("Materials using the MainMenu/* shaders. Element 0 shows first.")]
    public Material[] wallpapers;

    [Header("Behaviour")]
    [Range(0.01f, 1f)] public float mouseSmoothing = 0.12f;
    [Tooltip("Distance in front of the camera to place the quad.")]
    public float quadDistance = 10f;
    [Tooltip("Allow number / arrow keys to cycle wallpapers.")]
    public bool keyboardSwitching = true;

    [Header("State")]
    public int current = 0;

    static readonly int ID_Mouse     = Shader.PropertyToID("_Mouse");
    static readonly int ID_ClickPos  = Shader.PropertyToID("_ClickPos");
    static readonly int ID_ClickTime = Shader.PropertyToID("_ClickTime");
    static readonly int ID_MouseDown = Shader.PropertyToID("_MouseDown");
    static readonly int ID_Aspect    = Shader.PropertyToID("_Aspect");
    static readonly int ID_Holes     = Shader.PropertyToID("_Holes");
    static readonly int ID_HoleCount = Shader.PropertyToID("_HoleCount");

    // Ring buffer of recent left-click impacts, fed to shaders that read _Holes
    // (e.g. MainMenu/Ballistics). Each entry: xy = screen pos 0..1, z = age (s).
    const int MAX_HOLES = 16;
    readonly Vector4[] _holes      = new Vector4[MAX_HOLES];
    readonly float[]   _holeBorn   = new float[MAX_HOLES];
    int _holeHead = 0;

    Vector2 _mouse    = new Vector2(0.5f, 0.5f);
    Vector2 _target   = new Vector2(0.5f, 0.5f);
    Vector2 _clickPos = new Vector2(0.5f, 0.5f);
    float   _clickStart = -1000f;
    Material _active;

    void OnEnable()
    {
        if (cam == null) cam = Camera.main;
        // Park every impact slot far offscreen and "long ago" so nothing renders.
        for (int i = 0; i < MAX_HOLES; i++)
        {
            _holes[i]    = new Vector4(-10f, -10f, 9999f, 0f);
            _holeBorn[i] = -10000f;
        }
        Apply(current);
    }

    public void SetWallpaper(int index)
    {
        if (wallpapers == null || wallpapers.Length == 0) return;
        current = ((index % wallpapers.Length) + wallpapers.Length) % wallpapers.Length;
        Apply(current);
    }

    void Apply(int index)
    {
        if (wallpapers == null || index < 0 || index >= wallpapers.Length) return;
        _active = wallpapers[index];
        if (wallpaperRenderer != null) wallpaperRenderer.sharedMaterial = _active;
        _clickStart = -1000f; // start the freshly shown wallpaper calm
    }

    void Update()
    {
        if (_active == null) { Apply(current); if (_active == null) return; }
        if (cam == null) cam = Camera.main;

        FitQuadToCamera();

        // In the editor (not playing) just keep the quad fitted + aspect right,
        // and don't spam the shared material with per-frame input writes.
        if (cam != null && _active != null)
            _active.SetFloat(ID_Aspect, (float)Screen.width / Mathf.Max(1, Screen.height));
        if (!Application.isPlaying) return;

        // --- mouse position, normalized 0..1 (Unity screen origin is bottom-left) ---
        Vector3 mp = Input.mousePosition;
        _target = new Vector2(
            Mathf.Clamp01(mp.x / Mathf.Max(1, Screen.width)),
            Mathf.Clamp01(mp.y / Mathf.Max(1, Screen.height)));
        _mouse = Vector2.Lerp(_mouse, _target, mouseSmoothing);

        if (Input.GetMouseButtonDown(0))
        {
            _clickPos   = _target;
            _clickStart = Time.time;

            // Punch a new bullet hole into the ring buffer (w = 1 marks it active).
            _holes[_holeHead]    = new Vector4(_target.x, _target.y, 0f, 1f);
            _holeBorn[_holeHead] = Time.time;
            _holeHead = (_holeHead + 1) % MAX_HOLES;
        }
        float mouseDown = Input.GetMouseButton(0) ? 1f : 0f;

        // --- feed the active material ---
        _active.SetVector(ID_Mouse,    new Vector4(_mouse.x, _mouse.y, 0, 0));
        _active.SetVector(ID_ClickPos, new Vector4(_clickPos.x, _clickPos.y, 0, 0));
        _active.SetFloat (ID_ClickTime, Time.time - _clickStart);
        _active.SetFloat (ID_MouseDown, mouseDown);
        _active.SetFloat (ID_Aspect, (float)Screen.width / Mathf.Max(1, Screen.height));

        // --- feed the impact buffer to shaders that use it (Ballistics) ---
        // Detected via the hidden _HoleCount property: a uniform ARRAY (_Holes)
        // can't be declared in a shader Properties block, so HasProperty(_Holes)
        // is always false — _HoleCount is the reliable sentinel.
        if (_active.HasProperty(ID_HoleCount))
        {
            for (int i = 0; i < MAX_HOLES; i++)
                _holes[i].z = Time.time - _holeBorn[i];   // refresh ages
            _active.SetVectorArray(ID_Holes, _holes);
            _active.SetFloat(ID_HoleCount, MAX_HOLES);
        }

        // --- optional wallpaper switching ---
        if (keyboardSwitching && wallpapers != null)
        {
            for (int k = 0; k < wallpapers.Length && k < 9; k++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + k)) SetWallpaper(k);
            if (Input.GetKeyDown(KeyCode.RightArrow)) SetWallpaper(current + 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow))  SetWallpaper(current - 1);
        }
    }

    // Position and scale the quad so it exactly fills the camera view.
    void FitQuadToCamera()
    {
        if (quad == null || cam == null) return;

        // Match the camera's orientation exactly so quad +X = screen right and
        // +Y = screen up. Its front face (-Z normal) then points back at the
        // camera and its UVs line up with screen space (and with _Mouse).
        quad.position = cam.transform.position + cam.transform.forward * quadDistance;
        quad.rotation = cam.transform.rotation;

        float h, w;
        if (cam.orthographic)
        {
            h = cam.orthographicSize * 2f;
            w = h * cam.aspect;
        }
        else
        {
            h = 2f * quadDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            w = h * cam.aspect;
        }
        quad.localScale = new Vector3(w, h, 1f);
    }
}
