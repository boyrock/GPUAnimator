using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureAnimations : MonoBehaviour {

    [SerializeField]
    BakedTextureAnimation[] animations;


    public BakedTextureAnimation[] Animations
    {
        get
        {
            return animations;
        }
    }

    public BakedTextureAnimation Find(int hash)
    {
        for (int i = 0; i < animations.Length; i++)
        {
            if (animations[i].fullPathHash == hash)
                return animations[i];
        }

        return null;
    }

    public void SetItemSource(List<BakedTextureAnimation> bakedTextureAnimations)
    {
        animations = bakedTextureAnimations.ToArray();
    }

    //   // Use this for initialization
    //   void Start () {

    //}

    //// Update is called once per frame
    //void Update () {

    //}
}

[System.Serializable]
public class BakedTextureAnimation
{
    public int fullPathHash;
    public string animationName;
    public Texture positionAnimTexture;
    public Texture normalAnimTexture;
    public Vector4 texelSize;
}