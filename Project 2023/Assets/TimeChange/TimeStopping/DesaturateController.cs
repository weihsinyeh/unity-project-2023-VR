﻿using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
//using Luminosity.IO;
using Valve.VR;

public class DesaturateController : MonoBehaviour {

    public SteamVR_Action_Boolean TimeStop;


    //  [SerializeField] private UniversalRendererData rendererData = null;
    //  [SerializeField] private string featureName = null;
    [SerializeField] private float transitionPeriod = 1;


    public bool CanStop;
    private bool transitioning;
    private float startTime;
    private float fullscreenintensity;
  //  ScriptableRendererFeature feature;
    public Material mat;
    public bool TimeIsStopped;

    [Space]
    public List<ParticleSystem> allParticle;


    private void Start()
    {
        CanStop = false;
  //      feature = rendererData.rendererFeatures.Where((f) => f.name == featureName).FirstOrDefault();
  //      var blitFeature = feature as BlitMaterialFeature;
  //      mat = blitFeature.Material;
        mat.SetFloat("_Saturation", 1);
        mat.SetFloat("_FullScreenIntensity", 1);
    }

    private void Update() {
        if (CanStop)
        {
            //if (InputManager.GetButtonDown("TimeStop"))
            if (TimeStop.GetStateDown(SteamVR_Input_Sources.LeftHand))
            {
                if (!transitioning)
                {
                    PauseTime();
                }
                else
                {
                    ResumeTime();
                }
            }
            else if (transitioning)
            {
                if (Time.timeSinceLevelLoad >= startTime + transitionPeriod)
                {
                    ContinueTime();
                    ResetTransition();
                }
                else
                {
                    UpdateTransition();
                }
            }
        }
    }

    public void PauseTime()
    {
        Debug.Log("TimeStop");
        StartTransition();
        StopTime();
    }

    public void ResumeTime()
    {
        ContinueTime();
        ResetTransition();
    }


    //GrayScale Screen
    private void StartTransition() {
        startTime = Time.timeSinceLevelLoad;
        transitioning = true;
        fullscreenintensity = 0.4f;
    }
    private void UpdateTransition() {
            float saturation = Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / transitionPeriod);
            fullscreenintensity = Mathf.Lerp(fullscreenintensity,0.9f,0.002f);
            mat.SetFloat("_Saturation", saturation);
            mat.SetFloat("_FullScreenIntensity",fullscreenintensity);
    }
    public void ResetTransition() {
            mat.SetFloat("_Saturation", 1);
            mat.SetFloat("_FullScreenIntensity", 1);
            transitioning = false;
    }


    //for stopping objects
    public void ContinueTime()      //timestop finish
    {
        TimeIsStopped = false;

        var objects = FindObjectsOfType<TimeBody>();  //Find Every object with the Timebody Component
        for (var i = 0; i < objects.Length; i++)
        {
            objects[i].GetComponent<TimeBody>().ContinueTime(); //continue time in each of them
        }

        for (int i = 0; i < allParticle.Count; i++)
        {
            allParticle[i].Play(true);
        }

    }
    public void StopTime()
    {
        TimeIsStopped = true;
        for (int i = 0; i < allParticle.Count; i++) {
            allParticle[i].Pause(true);
        }
    }

}
