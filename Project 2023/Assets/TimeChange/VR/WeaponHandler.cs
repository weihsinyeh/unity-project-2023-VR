using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class WeaponHandler : MonoBehaviour
{
    // Start is called before the first frame update

    private Interactable currentInteractable;
    public Hand hand;
    public List<GameObject> weaponList;

    void Start()
    {
        currentInteractable = weaponList[0].GetComponent<Interactable>();
        hand.AttachObject(weaponList[0], GrabTypes.None);
        hand.HoverLock(currentInteractable);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
