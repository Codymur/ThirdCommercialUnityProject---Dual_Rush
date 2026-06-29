using UnityEngine;

[ExecuteAlways]
public class MoltenCardClicks : MonoBehaviour
{
    public Material material;     // assign the MainMenu/Molten material
    public Camera uiCamera;     // camera whose viewport gives you 0..1 uv (usually Camera.main)

    const int NUM_CARDS = 5;
    const float EXPLOSION_LIFE = 1.6f;

    float[] deadTime = new float[NUM_CARDS]; // _Time.y space; -1 = alive

    static readonly int IDA = Shader.PropertyToID("_DeadCards");
    static readonly int IDB = Shader.PropertyToID("_DeadCardsB");

    void OnEnable()
    {
        for (int i = 0; i < NUM_CARDS; i++) deadTime[i] = -1f;
        Push();
    }

    void Update()
    {
        if (material == null) return;
        if (uiCamera == null) uiCamera = Camera.main;

        // Respawn cards whose phase has wrapped past 1.0 since they were killed.
        float t = Time.timeSinceLevelLoad * Mathf.Max(0.0001f, material.GetFloat("_Speed"));
        for (int i = 0; i < NUM_CARDS; i++)
        {
            if (deadTime[i] < 0f) continue;
            float spd = CardSpeed(i);
            float seed = Hash21(new Vector2(i, 1.7f));
            float phaseDead = deadTime[i] * spd + seed;
            float phaseNow = t * spd + seed;
            if (Mathf.Floor(phaseNow) > Mathf.Floor(phaseDead)) deadTime[i] = -1f;
        }

        if (Input.GetMouseButtonDown(0) && uiCamera != null)
        {
            Vector2 uv = new Vector2(Input.mousePosition.x / Screen.width,
                                     Input.mousePosition.y / Screen.height);

            float aspect = material.GetFloat("_Aspect");
            float scale = Mathf.Max(0.0001f, material.GetFloat("_Scale"));
            Vector2 clkUV = new Vector2((uv.x - 0.5f) * aspect, (uv.y - 0.5f)) / scale;

            float now = Time.timeSinceLevelLoad * Mathf.Max(0.0001f, material.GetFloat("_Speed"));

            for (int i = 0; i < NUM_CARDS; i++)
            {
                if (deadTime[i] >= 0f) continue;
                CardAt(i, now, out Vector2 c, out Vector2 hs, out float rot, out float fade);
                if (fade < 0.25f) continue;

                Vector2 q = clkUV - c;
                float s = Mathf.Sin(rot), co = Mathf.Cos(rot);
                Vector2 ql = new Vector2(co * q.x - s * q.y, s * q.x + co * q.y);

                if (Mathf.Abs(ql.x) < hs.x && Mathf.Abs(ql.y) < hs.y)
                {
                    deadTime[i] = now;
                    break;
                }
            }
        }

        Push();
    }

    void Push()
    {
        material.SetVector(IDA, new Vector4(deadTime[0], deadTime[1], deadTime[2], deadTime[3]));
        material.SetVector(IDB, new Vector4(deadTime[4], -1f, -1f, -1f));
    }

    float CardSpeed(int i)
    {
        float seed = Hash21(new Vector2(i, 1.7f));
        return 0.045f + 0.05f * seed;
    }

    void CardAt(int i, float t, out Vector2 c, out Vector2 hs, out float rot, out float fade)
    {
        float fi = i;
        float seed = Hash21(new Vector2(fi, 1.7f));
        float spd = 0.045f + 0.05f * seed;
        float prog = Frac(t * spd + seed);
        float yPos = Mathf.Lerp(1.15f, -0.72f, prog);
        float lane = (fi + 0.5f) / NUM_CARDS;
        lane += (Hash21(new Vector2(fi, 3.1f)) - 0.5f) * 0.25f;
        float xPos = (lane - 0.5f) * 1.9f + 0.05f * Mathf.Sin(t * 0.3f + fi * 2.0f);
        rot = (seed - 0.5f) * 0.7f + 0.12f * Mathf.Sin(t * 0.25f + fi);
        hs = new Vector2(0.075f, 0.105f) * (0.75f + 0.7f * seed);
        c = new Vector2(xPos, yPos);
        fade = Mathf.Sin(prog * Mathf.PI);
    }

    static float Frac(float v) { return v - Mathf.Floor(v); }

    static float Hash21(Vector2 p)
    {
        p = new Vector2(Frac(p.x * 123.34f), Frac(p.y * 345.45f));
        float dot = Vector2.Dot(p, p + new Vector2(34.345f, 34.345f));
        p += new Vector2(dot, dot);
        return Frac(p.x * p.y);
    }
}