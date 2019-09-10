using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField] Kayac.SkinnedInstancingRenderer myRenderer;
    [SerializeField] MeshFilter originalMeshFilter;
    [SerializeField] int count;
    [SerializeField] Camera myCamera;
    [SerializeField] GameObject cubePrefab;

    struct State
    {
        public void Update(float deltaTime)
        {
            velocity *= 0.98f;
            velocity.y -= 9.81f * deltaTime;
            position += velocity * deltaTime;
            rotation = UpdateRotation(rotation, angularVelocity, deltaTime);
        }
        public void Update(ref Matrix4x4 matrix, float deltaTime)
        {
            Update(deltaTime);
            matrix.SetTRS(position, rotation, Vector3.one);
        }
        static Quaternion UpdateRotation(Quaternion orientation, Vector3 angularVelocity, float deltaTime)
        {
            var t = angularVelocity * (0.5f * deltaTime);
            var dq = orientation * new Quaternion(t.x, t.y, t.z, 0f);
            var q = new Quaternion(
                orientation.x + dq.x,
                orientation.y + dq.y,
                orientation.z + dq.z,
                orientation.w + dq.w);
            return q.normalized;
        }
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Quaternion rotation;
    }
    State[] states;
    bool pause;
    bool useSkinning = true;
    GameObject[] cubes;

    void Start()
    {
        Reset();
    }

    void Reset()
    {
        if (cubes != null)
        {
            foreach (var item in cubes)
            {
                Destroy(item);
            }
            cubes = null;
        }

        if (useSkinning)
        {
            myRenderer.gameObject.SetActive(true);
            myRenderer.ManualStart(originalMeshFilter.sharedMesh, count);
        }
        else
        {
            myRenderer.gameObject.SetActive(false);
            cubes = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                cubes[i] = Instantiate(cubePrefab, transform, false);
            }
        }
        states = new State[count];

        ResetTransforms();
    }

    void ResetTransforms()
    {
        var unit = Mathf.FloorToInt(Mathf.Pow((float)count, 1f / 3f));
        var offset = -(unit * 2f) / 2f;
        for (int i = 0; i < count; i++)
        {
            var t = i;
            var x = t / (unit * unit);
            t -= x * unit * unit;
            var y = t / unit;
            t -= y * unit;
            var z = t;
            states[i].position = new Vector3(x * 2f + offset, y * 2f, z * 2f + offset);
            states[i].rotation = Quaternion.identity;
        }
        if (useSkinning)
        {
            var poses = myRenderer.BeginUpdatePoses();
            for (int i = 0; i < count; i++)
            {
                poses[i].SetTRS(states[i].position, states[i].rotation, Vector3.one);
            }
            myRenderer.EndUpdatePoses();
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var t = cubes[i].transform;
                t.localPosition = states[i].position;
                t.localRotation = states[i].rotation;
            }
        }
        myCamera.transform.localPosition = new Vector3(0f, 10f, -40f);
        pause = true;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Explosion"))
        {
            ResetTransforms();
            StartExplosion();
        }
        if (GUILayout.Button("Reset"))
        {
            ResetTransforms();
        }
        var newUseSkininng = GUILayout.Toggle(useSkinning, "UseSkininng");
        if (newUseSkininng != useSkinning)
        {
            useSkinning = newUseSkininng;
            Reset();
        }
    }

    void StartExplosion()
    {
        for (int i = 0; i < states.Length; i++)
        {
            states[i].velocity = new Vector3(
                UnityEngine.Random.Range(-100f, 100f),
                UnityEngine.Random.Range(0f, 200f),
                UnityEngine.Random.Range(-100f, 100f));
            states[i].angularVelocity = new Vector3(
                UnityEngine.Random.Range(-10f, 10f),
                UnityEngine.Random.Range(-10f, 10f),
                UnityEngine.Random.Range(-10f, 10f));
        }
        pause = false;
    }


    void Update()
    {
        if (!pause)
        {
            float dt = Time.deltaTime;
            if (useSkinning)
            {
                var poses = myRenderer.BeginUpdatePoses();
                for (int i = 0; i < states.Length; i++)
                {
                    states[i].Update(ref poses[i], dt);
                }
                myRenderer.EndUpdatePoses();
            }
            else
            {
                for (int i = 0; i < states.Length; i++)
                {
                    states[i].Update(dt);
                    var t = cubes[i].transform;
                    t.localPosition = states[i].position;
                    t.localRotation = states[i].rotation;
                }
            }
            var p = myCamera.transform.localPosition;
            myCamera.transform.localPosition += (new Vector3(0f, 40f, -160f) - p) * (dt * 0.5f);
        }
    }
}
