﻿using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using Luminosity.IO;

public class TimeShiftingController : MonoBehaviour {
  //  [SerializeField] private UniversalRendererData rendererData = null;
  //  [SerializeField] private string featureName = null;
    [SerializeField] private float transitionPeriod = 1;




    //private bool transitioning;
    private float startTime;
  //  ScriptableRendererFeature feature;
    public Material mat;

    // public bool TimeIsStopped;


    //收縮強度
    [Range(0, 0.15f)]
    public float distortFactor = 1.0f;
    //扭曲中心
    public Vector2 distortCenter = new Vector2(0.5f, 0.5f);
    //噪聲圖
    public Texture NoiseTexture = null;
    public Camera mycamera;
    //屏幕擾動程度
    [Range(0, 2.0f)]
    public float distortStrength = 1.0f;
    //屏幕收縮總時間
    public float passThroughTime = 4.0f;
    //當前時間
    private float currentTime = 0.0f;
    //曲線控制權重
    public float scaleCurveFactor = 0.2f;
    //屏幕收縮效果曲線控制
    public AnimationCurve scaleCurve;
    //擾動曲線係數
    public float distortCurveFactor = 1.0f;
    //屏幕擾動效果曲線控制
    public AnimationCurve distortCurve;
    public Color AddColor;

    private Color baseColor;
    private int pastlayer;
    private int presentlayer;
    private int playerlayer;


    public bool CanChange;
    public int PastBool;  //0:present, 1:presentToPast 2:past 3:pastToPresent


    [Header("Environment")]
    public GameObject pastlight;
    public GameObject pastVolume;
    public Material PastSky;
    public Color PastFogColor;

    [Space]
    public GameObject presentlight;
    public GameObject presentVolume;
    public Material PresentSky;
    public Color PresentFogColor;


    private void Awake()
    {
        pastlayer = LayerMask.NameToLayer("Past");
        presentlayer = LayerMask.NameToLayer("Present");
        playerlayer = LayerMask.NameToLayer("Player");
        mycamera.cullingMask &= ~(1 << presentlayer);
        Physics.IgnoreLayerCollision(playerlayer, presentlayer, true);
        PastBool = 2;

    //    feature = rendererData.rendererFeatures.Where((f) => f.name == featureName).FirstOrDefault();
    //    var blitFeature = feature as BlitMaterialFeature;
    //    mat = blitFeature.Material;
        mat.SetTexture("_NoiseTex", NoiseTexture);
        baseColor = new Color(1, 1, 1, 1);
        CanChange = false;
    }

    private void Update() {
        if (CanChange)
        {
            if (InputManager.GetButtonDown("TimeShift"))
            {
                StartPassThroughEffect();
            }
        }
    }

    public void StartPassThroughEffect()
    {
        if (PastBool == 0) PastBool = 1;
        else if (PastBool == 2) PastBool = 3;
        currentTime = 0.0f;
        StartCoroutine(UpdatePassThroughEffect());
    }

    private IEnumerator UpdatePassThroughEffect()
    {
        while (currentTime < passThroughTime)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / passThroughTime;
            //根據時間佔比在曲線(0.1)區間採樣，再乘以權重作為收縮係數
            distortFactor = scaleCurve.Evaluate(t) * scaleCurveFactor;
            distortStrength = distortCurve.Evaluate(t) * distortCurveFactor;

            mat.SetColor("_AddColor", Color.Lerp(baseColor,AddColor , currentTime));
            mat.SetVector("_DistortCenter", distortCenter);
            mat.SetFloat("_DistortFactor", distortFactor);
            mat.SetFloat("_DistortStrength", distortStrength);
            yield return null;
            //結束時強制設置為0;

            distortFactor = 0.0f;
            distortStrength = 0.0f;
            mat.SetFloat("_DistortFactor", distortFactor);
            mat.SetFloat("_DistortStrength", distortStrength);
            mat.SetColor("_AddColor", baseColor);
            if (currentTime >= (passThroughTime - 0.5f) && PastBool == 1)
            {
                if(ObjectControl.controledObject!=null && ObjectControl.controledObject.layer == presentlayer)  //bring the object during time shifting
                {
                    ObjectControl.controledObject.layer = pastlayer;
                } 
                var cameras = FindObjectsOfType<Camera>();
                for (int i = 0; i < cameras.Length; i++)
                {
                    cameras[i].cullingMask |= (1 << pastlayer);
                    cameras[i].cullingMask &= ~(1 << presentlayer);
                }
                ChangeSky(PastSky, PastFogColor, pastlight, pastVolume);
                presentlight.SetActive(false);
                presentVolume.SetActive(false);
                Physics.IgnoreLayerCollision(playerlayer, pastlayer, false); Physics.IgnoreLayerCollision(playerlayer, presentlayer, true);
                PastBool = 2;
            }  //加pastlayer, 減presentlayer
            else if (currentTime >= (passThroughTime - 0.5f) && PastBool == 3)
            {
                if (ObjectControl.controledObject != null && ObjectControl.controledObject.layer == pastlayer) //bring the object during time shifting
                {
                    ObjectControl.controledObject.layer = presentlayer;
                }  
                var cameras = FindObjectsOfType<Camera>();
                for (int i = 0; i < cameras.Length; i++)
                {
                    cameras[i].cullingMask &= ~(1 << pastlayer);
                    cameras[i].cullingMask |= (1 << presentlayer);
                }
                ChangeSky(PresentSky, PresentFogColor, presentlight, presentVolume);
                pastlight.SetActive(false);
                pastVolume.SetActive(false);
                Physics.IgnoreLayerCollision(playerlayer, pastlayer, true); Physics.IgnoreLayerCollision(playerlayer, presentlayer, false);
                PastBool = 0;
            }//減pastlayer, 加presentlayer
        }
    }

    private void ChangeSky(Material Sky, Color FogColor, GameObject light, GameObject Volume) {
        RenderSettings.skybox = Sky;
        RenderSettings.fogColor = FogColor;
        light.SetActive(true) ;
        Volume.SetActive(true);
    }



}




