using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luminosity.IO;

public class PlayerCamVR : MonoBehaviour
{
    //public Transform headpoint;
    public Transform tf;
    public Transform playerVR;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MouseControl();
        MoveCam();
    }

    void MouseControl()
    {
        if (!InputManager.GetButton("Slash") && !PlayerMovement.isTransport)
        {
            PlayerMovement.onWall = false;
            if (!PlayerMovement.onWall)
            {
                tf.rotation = Quaternion.Euler(tf.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, tf.rotation.eulerAngles.z); //¤Hª«ªºrotation  yRotation
                                                                                                                                  // Debug.Log("CameraControl");
            }
        }
    }

    void MoveCam()
    {

        playerVR.transform.localPosition = tf.localPosition;
        //transform.localPosition = headpoint.localPosition;

    }







}
