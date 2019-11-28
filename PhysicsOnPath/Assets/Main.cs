using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Main : MonoBehaviour, IDragHandler
{
    [SerializeField] LineRenderer line;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] int cubeCount;
    [SerializeField] Sphere sphere;
    [SerializeField] Transform groundTransform;
    [SerializeField] float speed;
    [SerializeField] float forceFactor;
    [SerializeField] bool resetSwitch;
    [SerializeField] bool moveSwitch;
    [SerializeField] bool useMovePosition;

    [SerializeField] List<Vector3> path; // 見たいだけ
    [SerializeField] int pathIndex = -1; // 見たいだけ

    GameObject[] cubes;

    void Update()
    {
        if (resetSwitch)
        {
            resetSwitch = false;
            Start();
        }
        if (moveSwitch)
        {
            moveSwitch = false;
            sphere.Rigidbody.MovePosition(path[0]);
            pathIndex = 1;
        }
        if (pathIndex >= 0)
        {
            Move();
        }
    }

    void Move()
    {
        float t = Time.deltaTime * speed;
        while (t > 0)
        {
            t = MoveStep(t);
        }
    }

    float MoveStep(float t)
    {
        if (pathIndex >= path.Count) // 最後まで行った
        {
            return 0f; // 完了
        }
        // 次の目的値を取得
        var g = path[pathIndex];
        // 現在値
        var p = sphere.Rigidbody.position;
        // 移動ベクトル
        var moveVector = g - p;
        moveVector.y = 0f; // yは0
        var l = moveVector.magnitude; // 長さ
        Vector3 moveTo;
        if (l < t) // 長さがtより短ければ、ゴールまで移動
        {
            moveTo = new Vector3(g.x, 0.5f, g.z);
            pathIndex++;
            t -= l;
        }
        else // 辿りつかなかった
        {
            moveTo = p + (moveVector * (t / l));
            t = 0f;
        }
        if (useMovePosition)
        {
            sphere.Rigidbody.MovePosition(moveTo);
        }
        else
        {
            sphere.Rigidbody.AddForce(moveVector * (speed * forceFactor * sphere.Rigidbody.mass / l));
            sphere.Rigidbody.AddForce(sphere.Force);
            sphere.Force = Vector3.zero;
        }
        return t;
    }

    void Start()
    {
        path.Clear();
        pathIndex = -1;
        sphere.transform.position = new Vector3(0f, 0.5f, 0f);
        if (cubes != null)
        {
            foreach (var cube in cubes)
            {
                Destroy(cube);
            }
        }
        cubes = new GameObject[cubeCount];
        var groundScale = groundTransform.localScale;
        var sx = groundScale.x * 0.5f;
        var sz = groundScale.z * 0.5f;
        for (int i = 0; i < cubeCount; i++)
        {
            cubes[i] = Instantiate(cubePrefab, transform, false);
            cubes[i].transform.position = new Vector3(
                Random.Range(-sx, sx),
                0.5f,
                Random.Range(-sz, sz));
            cubes[i].transform.rotation = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);
        }
        line.positionCount = 0;
    }

    public void OnDrag(PointerEventData data)
    {
        var p = data.pointerCurrentRaycast.worldPosition;
        p.y = 0f;
        path.Add(p);
        line.positionCount = path.Count;
        p.y = 0.01f;
        line.SetPosition(path.Count - 1, p);
    }
}
