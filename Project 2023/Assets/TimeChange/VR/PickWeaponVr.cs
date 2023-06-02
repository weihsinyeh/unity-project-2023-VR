﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PickWeaponVr : MonoBehaviour
{
    private Interactable interactable;
    public bool Grabbed = false;

    public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.TurnOnKinematic;
    public Transform attachmentOffset;

    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void HandHoverUpdate(Hand hand)
    {

        if (!Grabbed)
        {

            GrabTypes grabType = hand.GetGrabStarting();
            //bool isGrabEnding = hand.IsGrabEnding(gameObject);
            if (interactable.attachedToHand == null && grabType == GrabTypes.Grip)
            {
                hand.AttachObject(gameObject, grabType, attachmentFlags, attachmentOffset);
                hand.HoverLock(interactable);           //锁定手部对物体的悬停（hover），防止其他物体的悬停操作
                                                        //  hand.HideGrabHint();
                Grabbed = true;
            }
          // else if (isGrabEnding)
          // {
          //     hand.DetachObject(gameObject);
          //     hand.HoverUnlock(interactable);
          // }
        }

    }
}
