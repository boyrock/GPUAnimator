using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUAnimator : MonoBehaviour {

    ComputeBuffer positionBuffer;
    ComputeBuffer normalBuffer;

    Vector3[] positions;
    Vector3[] normals;

    [SerializeField]
    ComputeShader kernelShader;

    Renderer _renderer;
    MeshFilter mf;

    int vertexCount;

    float animationTime;

    TextureAnimations textureAnimations;
    Animator animator;

    int updateAnimation_kernelIndex;
    int transitionAnimation_kernelIndex;

    MaterialPropertyBlock block;


    BakedTextureAnimation prev_anim;
    BakedTextureAnimation curr_anim;

    BakedTextureAnimation prev_next_anim;
    BakedTextureAnimation next_anim;
    // Use this for initialization
    void Start ()
    {
        animator = this.GetComponent<Animator>();
        _renderer = this.GetComponent<Renderer>();
        mf = this.GetComponent<MeshFilter>();
        textureAnimations = this.GetComponent<TextureAnimations>();

        vertexCount = mf.mesh.vertexCount;

        InitKernelIndex();

        InitBuffer();
    }

    private void InitKernelIndex()
    {
        updateAnimation_kernelIndex = kernelShader.FindKernel("UpdateAnimation");
        transitionAnimation_kernelIndex = kernelShader.FindKernel("TransitionAnimation");
    }

    private void InitBuffer()
    {
        positionBuffer = new ComputeBuffer(vertexCount, Marshal.SizeOf(typeof(Vector3)));
        positions = new Vector3[vertexCount];
        positionBuffer.SetData(positions);

        normalBuffer = new ComputeBuffer(vertexCount, Marshal.SizeOf(typeof(Vector3)));
        normals = new Vector3[vertexCount];
        normalBuffer.SetData(normals);
    }

    void Update () {

        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            if(Random.value > 0.5f)
                animator.SetBool("OnRun", true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            animator.SetBool("OnRun", false);
        }

        if (block == null)
            block = new MaterialPropertyBlock();

        var currAnimState = animator.GetCurrentAnimatorStateInfo(0);

        if (prev_anim == null || prev_anim.fullPathHash != currAnimState.fullPathHash)
        {
            curr_anim = textureAnimations.Find(currAnimState.fullPathHash);
        }
        else
        {
            curr_anim = prev_anim;
        }

        if (curr_anim == null)
            return;

        prev_anim = curr_anim;

        animationTime = currAnimState.normalizedTime;// Mathf.Repeat(, 1.0f);

        kernelShader.SetVector("TexelSize", curr_anim.texelSize);
        kernelShader.SetFloat("AnimationTime", animationTime);


        var nextAnimState = animator.GetNextAnimatorStateInfo(0);

        if (prev_next_anim == null || prev_next_anim.fullPathHash != nextAnimState.fullPathHash)
        {
            next_anim = textureAnimations.Find(nextAnimState.fullPathHash);
        }
        else
        {
            next_anim = prev_next_anim;
        }

        prev_next_anim = next_anim;

        if (next_anim != null && nextAnimState.normalizedTime > 0)
        {
            var transition =  animator.GetAnimatorTransitionInfo(0);

            kernelShader.SetTexture(transitionAnimation_kernelIndex, "PositionAnimTexture", curr_anim.positionAnimTexture);
            kernelShader.SetTexture(transitionAnimation_kernelIndex, "NormalAnimTexture", curr_anim.normalAnimTexture);

            kernelShader.SetTexture(transitionAnimation_kernelIndex, "PositionAnimTexture_Next", next_anim.positionAnimTexture);
            kernelShader.SetTexture(transitionAnimation_kernelIndex, "NormalAnimTexture_Next", next_anim.normalAnimTexture);
            kernelShader.SetVector("TexelSize_Next", next_anim.texelSize);

            kernelShader.SetFloat("AnimationTime_Next", nextAnimState.normalizedTime);

            kernelShader.SetFloat("TransitionTime", transition.normalizedTime);

            kernelShader.SetBuffer(transitionAnimation_kernelIndex, "PositionBuffer", positionBuffer);
            kernelShader.SetBuffer(transitionAnimation_kernelIndex, "NormalBuffer", normalBuffer);

            kernelShader.Dispatch(transitionAnimation_kernelIndex, vertexCount / 8 + 1, 1, 1);
        }
        else
        {
            kernelShader.SetTexture(updateAnimation_kernelIndex, "PositionAnimTexture", curr_anim.positionAnimTexture);
            kernelShader.SetTexture(updateAnimation_kernelIndex, "NormalAnimTexture", curr_anim.normalAnimTexture);

            kernelShader.SetBuffer(updateAnimation_kernelIndex, "PositionBuffer", positionBuffer);
            kernelShader.SetBuffer(updateAnimation_kernelIndex, "NormalBuffer", normalBuffer);

            kernelShader.Dispatch(updateAnimation_kernelIndex, vertexCount / 8 + 1, 1, 1);
        }

        block.SetBuffer("PositionBuffer", positionBuffer);
        block.SetBuffer("NormalBuffer", normalBuffer);

        _renderer.SetPropertyBlock(block);
    }

    private void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();

        if (normalBuffer != null)
            normalBuffer.Release();
    }
}
