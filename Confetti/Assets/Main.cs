using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
    [SerializeField] Camera camera;
    [SerializeField] Transform emitRoot;
    [SerializeField] float pieceWidth = 1f;
    [SerializeField] float pieceLength = 1f;
    [SerializeField] float normalBendRatio = 0.5f;
    [SerializeField] int step = 1;
    [SerializeField] SkinnedInstancingRenderer instanceRenderer;
    [SerializeField] MeshFilter sampleMeshFilter;

    Piece[] pieces;
    float pieceCountLog = Mathf.Log10(100f);
    float gravityLog = Mathf.Log10(20f);
    float windLog = Mathf.Log10(0.01f);
    float resistanceLog = Mathf.Log10(10f);
    Mesh originalMesh;

    void Start()
    {
        originalMesh = new Mesh()
        {
            name = "original"
        };
        MeshGenerator.GenerateQuad(
            originalMesh,
            Vector3.zero,
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            Vector2.zero,
            Vector2.zero,
            doubleSided: true);
        sampleMeshFilter.sharedMesh = originalMesh;
        Emit();
    }

    void Emit()
    {
        int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
        pieces = new Piece[pieceCount];
        var uvOffsets = new Vector2[pieceCount];

        Vector3 emitCenter = emitRoot.position;
        for (int i = 0; i < pieceCount; i++)
        {
            var q = Quaternion.identity;
            var qz = Quaternion.AngleAxis(Random.Range(-30f, 30f), new Vector3(0f, 0f, 1f));
            var qy = Quaternion.AngleAxis(Random.Range(-180f, 180f), new Vector3(0f, 1f, 0f));
            var qx = Quaternion.AngleAxis(Random.Range(-180f, 180f), new Vector3(1f, 0f, 0f));
//            q *= qz;
            q *= qy;
            q *= qx;
            var position = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f));
            var velocity = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(0f, 20f),
                Random.Range(-5f, 5f));
            var randomizedNormalBendRatio = Random.Range(normalBendRatio * 0.5f, normalBendRatio);
            pieces[i].Init(
                emitCenter + position,
                velocity,
                Time.deltaTime,
                q,
                pieceLength * 0.5f,
                randomizedNormalBendRatio);
            uvOffsets[i] = new Vector2(Random.Range(0.125f, 1f), 0f);
        }
        instanceRenderer.ManualStart(originalMesh, pieces.Length, uvOffsets);
    }

    void Update()
    {

        Vector3 wind;
        wind.x = -Mathf.Pow(10f, windLog);
        wind.y = wind.z = 0f;
        var gravity = new Vector3(0f, -Mathf.Pow(10f, gravityLog), 0f);
        float resistance = Mathf.Pow(10f, resistanceLog);
        float dt = Time.deltaTime / (float)step;
        float halfZSize = pieceLength * 0.5f;
        for (int stepIndex = 0; stepIndex < step; stepIndex++)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].Update(dt, ref wind, ref gravity, resistance, halfZSize);
            }
        }

        // 行列反映
        var poses = instanceRenderer.BeginUpdatePoses();
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].GetTransform(ref poses[i], pieceLength, pieceWidth);
        }
        instanceRenderer.EndUpdatePoses();

#if false
        Vector3 g = Vector3.zero;
        for (int i = 0; i < pieces.Length; i++)
        {
            var p = pieces[i].position;
            g += p;
        }
        g /= (float)pieces.Length;
        var sqDSum = 0f;
        for (int i = 0; i < pieces.Length; i++)
        {
            sqDSum += (pieces[i].position - g).sqrMagnitude;
        }
        var sd = Mathf.Sqrt(sqDSum / (float)pieces.Length);
        var c = new Vector3(g.x, g.y, g.z - (sd * 3f) - 10f);
        camera.transform.localPosition += (c - camera.transform.localPosition) * dt;
        camera.transform.LookAt(g);
#endif
    }

    void OnGUI()
    {
        int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
        GUI.Label(new Rect(0f, 0f, 120f, 30f), "Count: " + pieceCount);
        pieceCountLog = GUI.HorizontalSlider(
            new Rect(120f, 0f, 200f, 30f),
            pieceCountLog,
            0f,
            4f);

        float gravity = Mathf.Pow(10f, gravityLog);
        GUI.Label(new Rect(0f, 30f, 120f, 30f), "Gravity: " + gravity.ToString("F2"));
        gravityLog = GUI.HorizontalSlider(
            new Rect(120f, 30f, 200f, 30f),
            gravityLog,
            -2f,
            2f);

        float wind = Mathf.Pow(10f, windLog);
        GUI.Label(new Rect(0f, 60f, 120f, 30f), "Wind: " + wind.ToString("F2"));
        windLog = GUI.HorizontalSlider(
            new Rect(120f, 60f, 200f, 30f),
            windLog,
            -2f,
            2f);

        float resistance = Mathf.Pow(10f, resistanceLog);
        GUI.Label(new Rect(0f, 90f, 120f, 30f), "Resistance: " + resistance.ToString("F2"));
        resistanceLog = GUI.HorizontalSlider(
            new Rect(120f, 90f, 200f, 30f),
            resistanceLog,
            -3f,
            2f);

        GUI.Label(new Rect(0f, 120f, 120f, 30f), "NormalBend: " + normalBendRatio.ToString("F2"));
        normalBendRatio = GUI.HorizontalSlider(
            new Rect(120f, 120f, 200f, 30f),
            normalBendRatio,
            0f,
            1f);


        if (GUI.Button(new Rect(332f, 0f, 100f, 100f), "Emit"))
        {
            Emit();
        }
    }
}
