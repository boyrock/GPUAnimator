using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FlowerParticleSystem : MonoBehaviour {

    public Mesh mesh;
    public Bounds bound;
    public Shader shader;

    GPUAnimator gpuAnimator;
    Material _mat;

    ComputeBuffer positionBuffer;

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
    Vector3[] positions;


    // Use this for initialization
    void Start () {

        positionBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)));
        

        gpuAnimator = this.GetComponent<GPUAnimator>();

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        

    }

    //void UpdateBuffer()
    //{
    //    // indirect args
    //    uint numIndices = (mesh != null) ? (uint)mesh.GetIndexCount(0) : 0;
    //    args[0] = numIndices;
    //    args[1] = (uint)count;// (uint)gpuAnimator.positionBuffer.count;
    //    argsBuffer.SetData(args);
    //}

    void UpdateBuffers()
    {
        if(positionBuffer == null && gpuAnimator.positionBuffer.count > 0)
            positionBuffer = new ComputeBuffer(gpuAnimator.positionBuffer.count, Marshal.SizeOf(typeof(Vector3)));

        mat.SetBuffer("positionBuffer", gpuAnimator.positionBuffer);

        // indirect args
        uint numIndices = (mesh != null) ? (uint)mesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)gpuAnimator.positionBuffer.count;
        argsBuffer.SetData(args);


        //cachedInstanceCount = instanceCount;
    }

    // Update is called once per frame
    void Update () {

        UpdateBuffers();

        var mtx = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);
        mat.SetMatrix("transformMtx", mtx);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);
    }
}
