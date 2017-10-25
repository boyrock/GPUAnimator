using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class AnimationTextureBaker : MonoBehaviour
{
    readonly string folderName = "BakedAnimationTex";

    public ComputeShader infoTexGen;
    public Shader playShader;
    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
    }

    float time;
    SkinnedMeshRenderer skin;
    int vCount;
    Animation anim;
    int texWidth;
    Mesh mesh;

    int frames;

    RenderTexture pRt;
    RenderTexture nRt;

    List<BakedTextureAnimation> bakedTextureAnimations;

    GameObject go;

    string folderPath;

    // Use this for initialization
    void Start() { }

    public void PlayBake(string objectName)
    {
        anim = GetComponent<Animation>();
        skin = GetComponentInChildren<SkinnedMeshRenderer>();
        vCount = skin.sharedMesh.vertexCount;
        texWidth = Mathf.NextPowerOfTwo(vCount);
        mesh = new Mesh();
        bakedTextureAnimations = new List<BakedTextureAnimation>();

        CreateBaseDirectory();

        CreateObject(objectName);

        foreach (AnimationState state in anim)
        {
            StartCoroutine(Bake(state));
        }
    }

    private void CreateBaseDirectory()
    {
        folderPath = Path.Combine("Assets", folderName);

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets", folderName);
    }

    GameObject prefab;
    void CreateObject(string objectName)
    {
        Debug.Log("folderPath : " + folderPath);
        if (!AssetDatabase.IsValidFolder(folderPath))
            folderPath = AssetDatabase.CreateFolder(folderPath, objectName);

        //folderPath = Path.Combine(folderPath, objectName);

        var mat = new Material(playShader);
        mat.SetTexture("_MainTex", skin.sharedMaterial.mainTexture);

        AssetDatabase.CreateAsset(mat, Path.Combine(folderPath, string.Format("{0}.mat.asset", objectName)));

        go = new GameObject(objectName);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;
        go.AddComponent<Animator>();
        go.AddComponent<GPUAnimator>();

        go.AddComponent<TextureAnimations>();

        var prefabPath = Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/");

        prefab = PrefabUtility.CreatePrefab(prefabPath, go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private IEnumerator Bake(AnimationState state)
    {
        yield return new WaitForEndOfFrame();

        //Debug.Log("name" + state.name);

        anim.Play(state.name);
        frames = Mathf.NextPowerOfTwo((int)(state.length / 0.05f));
        var dt = state.length / frames;
        time = 0f;
        var infoList = new List<VertInfo>();

        //Debug.Log("dt : " + dt);

        pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        pRt.name = string.Format("{0}.{1}.posTex", name, state.name);
        nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        nRt.name = string.Format("{0}.{1}.normTex", name, state.name);
        foreach (var rt in new[] { pRt, nRt })
        {
            rt.enableRandomWrite = true;
            rt.Create();
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
        }

        //Debug.Log("frames : " + frames);

        for (var i = 0; i < frames; i++)
        {
            RecordAnimation(i, state, dt, infoList);
        }

        CreateAssets(state, infoList);
    }

    private void CreateAssets(AnimationState state, List<VertInfo> infoList)
    {
        var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
        buffer.SetData(infoList.ToArray());

        var kernel = infoTexGen.FindKernel("CSMain");
        uint x, y, z;
        infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

        infoTexGen.SetInt("VertCount", vCount);
        infoTexGen.SetBuffer(kernel, "Info", buffer);
        infoTexGen.SetTexture(kernel, "OutPosition", pRt);
        infoTexGen.SetTexture(kernel, "OutNormal", nRt);
        infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

        buffer.Release();

#if UNITY_EDITOR

        var posTex = RenderTextureToTexture2D.Convert(pRt);
        var normTex = RenderTextureToTexture2D.Convert(nRt);
        Graphics.CopyTexture(pRt, posTex);
        Graphics.CopyTexture(nRt, normTex);

        var bta = new BakedTextureAnimation();
        bta.fullPathHash = Animator.StringToHash(string.Format("Base Layer.{0}", state.name));
        bta.animationName = state.name;
        bta.positionAnimTexture = posTex;
        bta.normalAnimTexture = normTex;
        bta.texelSize = new Vector4(1.0f / posTex.width, 1.0f / posTex.height, posTex.width, posTex.height);
        bakedTextureAnimations.Add(bta);
        go.GetComponent<TextureAnimations>().SetItemSource(bakedTextureAnimations);

        AssetDatabase.CreateAsset(posTex, Path.Combine(folderPath, pRt.name + ".asset"));
        AssetDatabase.CreateAsset(normTex, Path.Combine(folderPath, nRt.name + ".asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        prefab = PrefabUtility.ReplacePrefab(go, prefab);
#endif
    }


    private void RecordAnimation(int index, AnimationState state, float dt, List<VertInfo> infoList)
    {
        state.time = time;
        anim.Sample();
        skin.BakeMesh(mesh);

        infoList.AddRange(Enumerable.Range(0, vCount)
            .Select(idx => new VertInfo()
            {
                position = mesh.vertices[idx],
                normal = mesh.normals[idx]
            })
        );

        time += dt;
    }
}
