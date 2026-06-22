using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PerkBackgroundTimeDriver : MonoBehaviour
{
    // If you're driving a UI Image material, swap Renderer for MeshRenderer
    // or use a MaterialPropertyBlock on a RawImage's material reference
    [SerializeField] private Renderer _target;
    [SerializeField] private Material _material; // assign if not using Renderer

    private static readonly int UnscaledTimeProp = Shader.PropertyToID("_UnscaledTime");

    private void Awake()
    {
        // Prefer explicit material slot; fall back to Renderer's sharedMaterial
        if (_material == null && _target != null)
            _material = _target.material; // creates an instance automatically
    }

    // Update() still fires when Time.timeScale == 0
    private void Update()
    {
        if (_material != null)
            _material.SetFloat(UnscaledTimeProp, Time.unscaledTime);
    }
}