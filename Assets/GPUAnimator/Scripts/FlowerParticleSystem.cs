using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FlowerParticleSystem : MonoBehaviour
{

    public Mesh instancedMesh;
    public Bounds bound;
    public Shader shader;

    GPUAnimator gpuAnimator;
    Material _mat;

    ComputeBuffer positionBuffer;
    ComputeBuffer triangleBuffer;

    Material mat
    {
        get
        {
            if (_mat == null)
                _mat = new Material(shader);

            return _mat;
        }
    }

    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    int count = 1;

    [SerializeField]
    Texture2D samplingPositionTex;

    [SerializeField]
    float sphereSize = 0.01f;

    Mesh mesh;
    MeshFilter mf;
    SkinnedMeshRenderer skinRenderer;

    // Use this for initialization
    void Start()
    {

        mf = this.GetComponent<MeshFilter>();
        skinRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();

        if (mf != null)
            mesh = mf.mesh;
        else
        {
            mesh = new Mesh();
            skinRenderer.BakeMesh(mesh);
        }


        gpuAnimator = this.GetComponent<GPUAnimator>();


        SetupBuffer();
    }

    private void SetupBuffer()
    {
        var positions = GetDataFromSamplingTex();

        positionBuffer = new ComputeBuffer(positions.Length, Marshal.SizeOf(typeof(Vector3)));
        positionBuffer.SetData(positions);

        triangleBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        triangleBuffer.SetData(mesh.triangles);

        // indirect args
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint numIndices = (instancedMesh != null) ? (uint)instancedMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)positions.Length;
        argsBuffer.SetData(args);
    }

    Vector3[] GetDataFromSamplingTex()
    {
        var pixels = samplingPositionTex.GetPixels();
        var positions = new Vector3[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            var col = pixels[i];

            if ((int)col.r > 0)
                positions[i] = new Vector3(col.r, col.g, col.b);
        }

        return positions;
    }

    Vector3 UvToWorldPosition(int index, float u, float v)
    {
        var t1 = mesh.triangles[index * 3];
        var t2 = mesh.triangles[index * 3 + 1];
        var t3 = mesh.triangles[index * 3 + 2];

        float aa = u;
        float bb = v;
        float cc = 1 - u - v;

        Vector3 p3D = aa * mesh.vertices[t1] + bb * mesh.vertices[t2] + cc * mesh.vertices[t3];

        p3D *= 1;

        return transform.TransformPoint(p3D);
    }

    void UpdateBuffers()
    {
        if (gpuAnimator == null)
            return;

        mat.SetBuffer("vertexBuffer", gpuAnimator.vertexBuffer);
        mat.SetBuffer("positionBuffer", positionBuffer);
        mat.SetBuffer("triangleBuffer", triangleBuffer);
    }


    // Update is called once per frame
    void Update()
    {

        UpdateBuffers();

        var mtx = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);
        mat.SetMatrix("transformMtx", mtx);

        Graphics.DrawMeshInstancedIndirect(instancedMesh, 0, mat, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);
    }

    private void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Dispose();
        if (triangleBuffer != null)
            triangleBuffer.Dispose();
    }
}
