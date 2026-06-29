using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PerkBackgroundTimeDriver : MonoBehaviour
{
    [SerializeField] private Renderer _target;
    [SerializeField] private Material _material; // assign to force a specific material

    private static readonly int UnscaledTimeProp = Shader.PropertyToID("_UnscaledTime");

    private float _origin;

    /// <summary>The exact material instance this driver writes to — share this everywhere.</summary>
    public Material ActiveMaterial { get { EnsureMaterial(); return _material; } }

    private void EnsureMaterial()
    {
        if (_material != null) return;
        if (_target == null) _target = GetComponent<Renderer>();
        if (_target != null) _material = _target.material;   // renderer's instanced material
    }

    private void Awake() { EnsureMaterial(); }

    // Reset the clock every time the background is shown.
    private void OnEnable()
    {
        EnsureMaterial();
        _origin = Time.unscaledTime;
        if (_material != null) _material.SetFloat(UnscaledTimeProp, 0f);
    }

    // Update() fires even at Time.timeScale == 0, and unscaledTime ignores timeScale.
    private void Update()
    {
        if (_material != null)
            _material.SetFloat(UnscaledTimeProp, Time.unscaledTime - _origin);
    }
}