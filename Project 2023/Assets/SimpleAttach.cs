using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SimpleAttach : MonoBehaviour
{
    private Interactable interactable;
    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        hand.ShowGrabHint();
    }

    private void OnHandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
    }
    private void HandHoverUpdate(Hand hand)
    {
        GrabTypes grabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(gameObject);

        if(grabType == null){Debug.Log("GrabType is null");}
        if(interactable.attachedToHand == null && grabType != GrabTypes.None )
        {
            Debug.Log("first if"+gameObject);
            hand.AttachObject(gameObject, grabType);
            hand.HoverLock(interactable);
            hand.HideGrabHint(); 
        }
        else if(isGrabEnding)
        {
            Debug.Log("Is Grab Ending"+gameObject);
            hand.DetachObject(gameObject);
            hand.HoverUnlock(interactable);
        }
        else{
            Debug.Log("Not enter");
        }
    }
}