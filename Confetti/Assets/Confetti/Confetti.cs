using UnityEngine;
using Kayac;

public class Confetti : MonoBehaviour
{
    [SerializeField] SkinnedInstancingRenderer instanceRenderer;
    [SerializeField] int pieceCount = 500;
    [SerializeField] float emitPiecePerSecond = 30f;
    [SerializeField] bool emissionLooped = true;
    [SerializeField] float positionRandomizeRadius;
    [SerializeField] float velocityRandomizeRadius;
    [SerializeField] float pieceWidth = 1f;
    [SerializeField] float pieceLength = 1f;
    [SerializeField] float normalBendRatio = 0.5f;
    [SerializeField] float resistance = 5f;
    [SerializeField] Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [SerializeField] Vector3 wind = Vector3.zero;
    [SerializeField] bool autoStart;

    public int PieceCount { get { return pieceCount; } }

    public float PositionRandomizeRadius
    {
        get
        {
            return positionRandomizeRadius;
        }
        set
        {
            positionRandomizeRadius = value;
        }
    }

    public float VelocityRandomizeRadius
    {
        get
        {
            return velocityRandomizeRadius;
        }
        set
        {
            velocityRandomizeRadius = value;
        }
    }

    public float PieceWidth
    {
        get
        {
            return pieceWidth;
        }
        set
        {
            pieceWidth = value;
        }
    }

    public float PieceLength
    {
        get
        {
            return pieceLength;
        }
        set
        {
            pieceLength = value;
        }
    }

    public float NormalBendRatio
    {
        get
        {
            return normalBendRatio;
        }
        set
        {
            normalBendRatio = value;
        }
    }

    public float Resistance
    {
        get
        {
            return resistance;
        }
        set
        {
            resistance = value;
        }
    }

    public Vector3 Gravity
    {
        get
        {
            return gravity;
        }
        set
        {
            gravity = value;
        }
    }

    public Vector3 Wind
    {
        get
        {
            return wind;
        }
        set
        {
            wind = value;
        }
    }

    public bool EmissionLooped
    {
        get
        {
            return emissionLooped;
        }
        set
        {
            emissionLooped = value;
        }
    }

    public float EmitPiecePerSecond
    {
        get
        {
            return emitPiecePerSecond;
        }
        set
        {
            emitPiecePerSecond = value;
        }
    }

    Mesh originalMesh;
    ConfettiPiece[] pieces;
    int emitIndex;
    float emitCarry; // 端数持ち越し

    private void Start()
    {
        if (autoStart)
        {
            ManualStart();
        }
    }

    public void ManualStart(int pieceCountOverride = 0)
    {
        if (pieceCountOverride > 0)
        {
            pieceCount = pieceCountOverride;
        }
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
        var uvOffsets = new Vector2[pieceCount];
        for (int i = 0; i < pieceCount; i++)
        {
            uvOffsets[i] = new Vector2(Random.Range(0.125f, 1f), 0f);
        }
        instanceRenderer.ManualStart(originalMesh, uvOffsets.Length, uvOffsets);
        // 実際にこの数で作れたとは限らないので確認して上書きする
        pieceCount = instanceRenderer.Count;
        pieces = new ConfettiPiece[pieceCount];
    }

    public void StartEmission()
    {
        if (originalMesh == null)
        {
            ManualStart();
        }
        emitCarry = 0f;
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].Init(
                Vector3.zero,
                Vector3.zero,
                0f,
                Quaternion.identity,
                0f,
                0f);
        }
    }

    void EmitPiece(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var q = Quaternion.identity;
            var qz = Quaternion.AngleAxis(Random.Range(-10f, 10f), new Vector3(0f, 0f, 1f));
            var qy = Quaternion.AngleAxis(Random.Range(-180f, 180f), new Vector3(0f, 1f, 0f));
            var qx = Quaternion.AngleAxis(Random.Range(-180f, 180f), new Vector3(1f, 0f, 0f));
            q *= qz;
            q *= qy;
            q *= qx;
            var position = new Vector3(
                Random.Range(-positionRandomizeRadius, positionRandomizeRadius),
                Random.Range(-positionRandomizeRadius, positionRandomizeRadius),
                Random.Range(-positionRandomizeRadius, positionRandomizeRadius));
            var velocity = new Vector3(
                Random.Range(-velocityRandomizeRadius, velocityRandomizeRadius),
                Random.Range(velocityRandomizeRadius, velocityRandomizeRadius),
                Random.Range(-velocityRandomizeRadius, velocityRandomizeRadius));
            var randomizedNormalBendRatio = Random.Range(normalBendRatio * 0.5f, normalBendRatio);
            pieces[emitIndex].Init(
                position,
                velocity,
                Time.deltaTime,
                q,
                pieceLength * 0.5f,
                randomizedNormalBendRatio);
            emitIndex++;
            if (emitIndex >= pieces.Length)
            {
                emitIndex = 0;
                if (!emissionLooped)
                {
                    emitPiecePerSecond = 0f;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (pieces == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        if (emitPiecePerSecond > 0f)
        {
            var countF = (emitPiecePerSecond * dt) + emitCarry;
            var countI = (int)countF;
            EmitPiece(countI);
            emitCarry = countF - (float)countI;
        }

        float halfZSize = pieceLength * 0.5f;
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].Update(dt, ref wind, ref gravity, resistance, halfZSize);
        }

        // 行列反映
        var poses = instanceRenderer.BeginUpdatePoses();
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].GetTransform(ref poses[i], pieceLength, pieceWidth);
        }
        instanceRenderer.EndUpdatePoses();
    }
}
