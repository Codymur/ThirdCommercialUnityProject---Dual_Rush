using UnityEngine;
using UnityEditor;

/// <summary>
/// Grouped material inspector for the MainMenu/* wallpaper shaders.
/// Splits the tweakable settings into labelled sections and draws a live
/// swatch strip for the selected palette so you can recolour by eye.
///
/// Place this file in any folder named "Editor" (e.g.
/// Assets/MainMenuShaders/Editor/). Each wallpaper shader references it with
///     CustomEditor "MenuWallpaperShaderGUI"
/// If this script is ever missing, Unity silently falls back to the default
/// material inspector — nothing breaks.
/// </summary>
public class MenuWallpaperShaderGUI : ShaderGUI
{
    // Palette previews — mirror the loadPalette() banks baked into the shaders.
    // Index 0 (Native) is shader-specific, so it is shown as a note instead.
    static readonly Color[][] Palettes =
    {
        null, // 0 Native (per-shader)
        new[]{ C(0.02f,0.03f,0.08f), C(0.02f,0.10f,0.22f), C(0.03f,0.20f,0.40f), C(0,0.42f,0.62f), C(0.05f,0.62f,0.82f), C(0.30f,0.85f,1f), C(0.65f,0.95f,1f), C(0.92f,0.99f,1f) }, // Ice
        new[]{ C(0.02f,0.01f,0.02f), C(0.16f,0.03f,0.02f), C(0.40f,0.07f,0.02f), C(0.70f,0.18f,0.02f), C(0.92f,0.38f,0.04f), C(1f,0.58f,0.12f), C(1f,0.80f,0.35f), C(1f,0.95f,0.70f) }, // Ember
        new[]{ C(0.01f,0.03f,0.02f), C(0.02f,0.10f,0.05f), C(0.03f,0.22f,0.09f), C(0.08f,0.40f,0.15f), C(0.25f,0.68f,0.22f), C(0.45f,0.92f,0.32f), C(0.70f,1f,0.55f), C(0.90f,1f,0.82f) }, // Toxic
        new[]{ C(0.02f,0.01f,0.05f), C(0.10f,0.02f,0.18f), C(0.28f,0.04f,0.36f), C(0.55f,0.08f,0.52f), C(0.85f,0.16f,0.62f), C(1f,0.30f,0.72f), C(0.45f,0.78f,1f), C(0.92f,0.95f,1f) }, // Synth
        new[]{ C(0.018f,0.022f,0.028f), C(0.07f,0.085f,0.10f), C(0.17f,0.19f,0.22f), C(0.34f,0.37f,0.41f), C(0.55f,0.58f,0.63f), C(0.78f,0.81f,0.86f), C(0.92f,0.94f,0.97f) }, // Steel
        new[]{ C(0.02f,0.02f,0.02f), C(0.18f,0.18f,0.18f), C(0.36f,0.36f,0.36f), C(0.55f,0.55f,0.55f), C(0.74f,0.74f,0.74f), C(1f,1f,1f) }, // Mono
        new[]{ C(0.03f,0.02f,0f), C(0.16f,0.10f,0.02f), C(0.36f,0.22f,0.05f), C(0.62f,0.42f,0.12f), C(0.85f,0.66f,0.28f), C(0.98f,0.85f,0.52f), C(1f,0.96f,0.80f) }, // Gold
        new[]{ C(0.05f,0.02f,0.08f), C(0.18f,0.07f,0.24f), C(0.45f,0.16f,0.45f), C(0.92f,0.35f,0.62f), C(1f,0.62f,0.66f), C(0.55f,0.92f,0.92f), C(0.80f,1f,0.95f) }, // Vapor
        new[]{ C(0.03f,0f,0f), C(0.12f,0.01f,0.01f), C(0.28f,0.02f,0.03f), C(0.50f,0.04f,0.05f), C(0.72f,0.10f,0.08f), C(0.90f,0.22f,0.14f), C(1f,0.45f,0.30f), C(1f,0.80f,0.70f) }, // Blood
        new[]{ C(0.04f,0.03f,0.02f), C(0.14f,0.10f,0.06f), C(0.30f,0.22f,0.12f), C(0.50f,0.38f,0.20f), C(0.70f,0.56f,0.32f), C(0.86f,0.74f,0.48f), C(0.96f,0.88f,0.66f), C(1f,0.97f,0.85f) }, // Desert
        new[]{ C(0f,0.02f,0f), C(0f,0.08f,0.02f), C(0f,0.18f,0.05f), C(0.02f,0.34f,0.10f), C(0.10f,0.55f,0.18f), C(0.25f,0.78f,0.30f), C(0.50f,0.95f,0.45f), C(0.85f,1f,0.80f) }, // NightVision
        new[]{ C(0.01f,0.02f,0.04f), C(0.02f,0.06f,0.12f), C(0.04f,0.12f,0.24f), C(0.06f,0.22f,0.40f), C(0.10f,0.36f,0.60f), C(0.20f,0.52f,0.80f), C(0.45f,0.72f,0.95f), C(0.80f,0.92f,1f) }, // Cobalt
        new[]{ C(0.02f,0f,0.05f), C(0.10f,0f,0.22f), C(0.30f,0f,0.40f), C(0.60f,0.04f,0.30f), C(0.85f,0.18f,0.10f), C(0.98f,0.50f,0.05f), C(1f,0.82f,0.20f), C(1f,0.98f,0.85f) }, // Thermal
        new[]{ C(0.02f,0.02f,0f), C(0.08f,0.07f,0.02f), C(0.16f,0.13f,0.03f), C(0.34f,0.26f,0.04f), C(0.60f,0.45f,0.05f), C(0.85f,0.66f,0.08f), C(1f,0.85f,0.15f), C(1f,0.96f,0.70f) }, // Hazard
        new[]{ C(0.03f,0.03f,0.02f), C(0.08f,0.09f,0.05f), C(0.14f,0.16f,0.08f), C(0.22f,0.26f,0.12f), C(0.34f,0.38f,0.18f), C(0.48f,0.52f,0.28f), C(0.66f,0.68f,0.42f), C(0.86f,0.86f,0.66f) }, // Military
        new[]{ C(0.015f,0.02f,0.03f), C(0.05f,0.07f,0.10f), C(0.10f,0.14f,0.19f), C(0.18f,0.24f,0.31f), C(0.30f,0.37f,0.45f), C(0.45f,0.53f,0.62f), C(0.66f,0.73f,0.82f), C(0.90f,0.94f,1f) }, // Gunmetal
        new[]{ C(0.03f,0.02f,0.01f), C(0.10f,0.05f,0.03f), C(0.22f,0.10f,0.05f), C(0.40f,0.18f,0.08f), C(0.60f,0.30f,0.12f), C(0.78f,0.44f,0.20f), C(0.90f,0.62f,0.38f), C(0.98f,0.84f,0.66f) }, // Rust
        new[]{ C(0.01f,0.02f,0.05f), C(0.04f,0.05f,0.12f), C(0.10f,0.09f,0.14f), C(0.24f,0.16f,0.10f), C(0.46f,0.30f,0.10f), C(0.72f,0.48f,0.12f), C(0.94f,0.70f,0.22f), C(1f,0.90f,0.55f) }, // Sodium
    };

    static readonly string[] PaletteNames =
        { "Native", "Ice", "Ember", "Toxic", "Synth", "Steel", "Mono", "Gold", "Vapor", "Blood", "Desert", "NightVision", "Cobalt", "Thermal", "Hazard", "Military", "Gunmetal", "Rust", "Sodium" };

    static Color C(float r, float g, float b) => new Color(r, g, b);

    public override void OnGUI(MaterialEditor me, MaterialProperty[] props)
    {
        var palette   = FindProperty("_Palette",        props, false);
        var dither    = FindProperty("_DitherStrength", props, false);
        var pixel     = FindProperty("_Pixelation",     props, false);
        var speed     = FindProperty("_Speed",          props, false);
        var scale     = FindProperty("_Scale",          props, false);
        var clickScl  = FindProperty("_ClickScale",     props, false);

        EditorGUIUtility.labelWidth = 170f;

        // ---- Coloring ----
        Section("Coloring");
        if (palette != null)
        {
            me.ShaderProperty(palette, "Palette");
            DrawSwatches(Mathf.RoundToInt(palette.floatValue));
        }
        if (dither != null) me.ShaderProperty(dither, "Dither Strength");

        // ---- Resolution ----
        Section("Resolution");
        if (pixel != null)
        {
            me.ShaderProperty(pixel, "Pixelation");
            EditorGUILayout.LabelField(" ", "0 = crisp · higher = chunkier pixels", EditorStyles.miniLabel);
        }

        // ---- Motion ----
        Section("Motion");
        if (speed != null) me.ShaderProperty(speed, "Motion Speed");

        // ---- Interaction & Layout ----
        Section("Interaction & Layout");
        if (scale != null)    me.ShaderProperty(scale, "Background Scale");
        if (clickScl != null) me.ShaderProperty(clickScl, "Click Impact Scale");

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Mouse position, click position/time and aspect are driven automatically " +
            "by MenuWallpaperController at runtime — no need to set them here.",
            MessageType.None);

        // ---- Advanced (render queue, instancing, GI) ----
        // Re-drawn here because a custom ShaderGUI replaces the default footer.
        // These shaders ship in the Transparent queue so they render AFTER the
        // skybox (otherwise ZWrite-Off + Geometry queue lets the skybox paint
        // over the quad and it looks invisible). Change it here if you need to.
        Section("Advanced");
        me.RenderQueueField();
        me.EnableInstancingField();
        me.DoubleSidedGIField();
    }

    static void Section(string title)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.25f));
        EditorGUILayout.Space(2f);
    }

    static void DrawSwatches(int index)
    {
        if (index <= 0 || index >= Palettes.Length || Palettes[index] == null)
        {
            EditorGUILayout.LabelField(" ", "Native — the shader's built-in palette", EditorStyles.miniLabel);
            return;
        }

        var cols = Palettes[index];
        Rect row = EditorGUILayout.GetControlRect(false, 22f);
        row.x += EditorGUIUtility.labelWidth - 14f;
        row.width -= EditorGUIUtility.labelWidth - 14f;

        float w = row.width / cols.Length;
        for (int i = 0; i < cols.Length; i++)
        {
            var cell = new Rect(row.x + i * w, row.y, w - 1f, row.height);
            EditorGUI.DrawRect(cell, cols[i]);
        }
        EditorGUI.LabelField(
            new Rect(EditorGUIUtility.labelWidth - 150f, row.y + 4f, 150f, 16f),
            PaletteNames[index], EditorStyles.miniLabel);
    }
}
