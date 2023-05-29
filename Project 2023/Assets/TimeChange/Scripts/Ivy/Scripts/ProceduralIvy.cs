using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luminosity.IO;
using UnityEngine.UI;
using Valve.VR;

public class ProceduralIvy : MonoBehaviour {
    public SteamVR_Action_Vector2 VerHor_input;
    public SteamVR_Action_Boolean FireVR;
    public SteamVR_Action_Boolean shortenRopeVR;


    [Space]
    public GameObject IvyObject;
    public Transform cam;
    [Space]
    public float recycleInterval = 30;
    [Space]
    public int branches = 3;
    public int maxPointsForBranch = 20;
    public float segmentLength = .002f;
    public float branchRadius = 0.02f;
    [Space]
    public Material branchMaterial;
    //   public Material leafMaterial;
    //   public Material flowerMaterial;
        [Space]
    //    public Blossom leafPrefab;
    //    public Blossom flowerPrefab;
        [Space]
     //   public bool wantBlossoms;
    
    int ivyCount = 0;
    int ignoreLayer;

    [Space]
        private LineRenderer lr;
        private Vector3 swingPoint;
        public float maxSwingDistance;
        public Transform gunTip, player;
        private SpringJoint joint;
        private Vector3 currentGrapplePosition;
        private PlayerMovement playerMovement;
        private float o_MoveSpeed;
        public float swingSpeed;
        private TimeShiftingController tmCon;
        public bool moving = false;

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSphereCastRadius;
  //  public Transform predictionPoint;
    [Header("OdmGear")]
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;
    public GameObject image;
    private Image im;
    private PickWeaponVr pickweapon;

    public AudioManager audioManager;
    void Awake()
      {
          lr = GetComponent<LineRenderer>();
          playerMovement = player.gameObject.GetComponent<PlayerMovement>();
          o_MoveSpeed = playerMovement.moveSpeed;
          ignoreLayer = ~((1 << 2) | (1 << 3) | (1 << 4)| (1 << 6));
          tmCon = GameObject.FindGameObjectWithTag("TimeManager").GetComponent<TimeShiftingController>();
          rb = player.GetComponent<Rigidbody>();
          im = image.GetComponent<Image>();
          pickweapon = GetComponent<PickWeaponVr>();


    }

    void Update() {

        if (pickweapon.Grabbed)
        {
            if (tmCon.PastBool == 0) ignoreLayer = ~((1 << 2) | (1 << 3) | (1 << 4) | (1 << 6) | (1 << 10));
            else if (tmCon.PastBool == 2) ignoreLayer = ~((1 << 2) | (1 << 3) | (1 << 4) | (1 << 6) | (1 << 9));

            if (joint)
            {
                if (tmCon.PastBool == 0 && (predictionHit.transform.gameObject.layer == 10)) StopGrapple();
                else if (tmCon.PastBool == 2 && (predictionHit.transform.gameObject.layer == 9)) StopGrapple();
                else if (!playerMovement.swinging) StopGrapple();
                OdmGearMovement();
            }

            if (FireVR.GetStateDown(SteamVR_Input_Sources.RightHand))
            {// InputManager.GetButtonDown("Fire")

                if (!joint)
                {
                    if (predictionHit.point == Vector3.zero) return;
                    StartSwing();
                }
                else
                {
                    StopGrapple();
                }
            }

            CheckForSwingPoints();
        }

    }

  void LateUpdate()
  {
      DrawRope();
  }

    //Grappleable: �i�����ͦ��ý�; IvyGenerateable:�i�ͦ��ý�
   private void CheckForSwingPoints()
   {
       if (joint != null) return;
        im.color = Color.white;

        RaycastHit sphereCastHit;
       Physics.SphereCast(gunTip.position, predictionSphereCastRadius, gunTip.forward,            //�����P��
                           out sphereCastHit, maxSwingDistance, ignoreLayer);
  
       RaycastHit raycastHit;
       Physics.Raycast(gunTip.position, gunTip.forward,                                           //�����˷�
                           out raycastHit, maxSwingDistance, ignoreLayer);

       // Vector3 realHitPoint;

        bool SphereGrab = false;
        if (raycastHit.point != Vector3.zero && sphereCastHit.point != Vector3.zero)           //��̬Ҧ�hit�A���P�w��sphere: �����I�S����tag������A���P�򦳨䤤�@��
            SphereGrab = (raycastHit.transform.gameObject.tag != "Grappleable" && raycastHit.transform.gameObject.tag != "IvyGenerateable")
                            && (sphereCastHit.transform.gameObject.tag == "Grappleable" || sphereCastHit.transform.gameObject.tag == "IvyGenerateable");

        // Option 1 - Direct Hit
        if (raycastHit.point != Vector3.zero && !SphereGrab)
        {
            predictionHit = raycastHit;
        }
        //realHitPoint = raycastHit.point;

        // Option 2 - Indirect (predicted) Hit
        else if (sphereCastHit.point != Vector3.zero)
        {
            predictionHit = sphereCastHit;
        }
        //realHitPoint = sphereCastHit.point;

        // Option 3 - Miss
        else
            predictionHit.point = Vector3.zero;


        if (predictionHit.point != Vector3.zero)
        {
            if(predictionHit.transform.gameObject.tag == "Grappleable")
                im.color = Color.red;
            else if(predictionHit.transform.gameObject.tag == "IvyGenerateable")
                im.color = Color.green;
        }
    }


    private void StartSwing() {
        if (predictionHit.point == Vector3.zero) return;

        if (predictionHit.transform.gameObject.tag == "IvyGenerateable")
        {
            audioManager.PlayAudio("generateVine");
            if (IvyObject.transform.childCount >= 5)
            {
                Clear();
                ivyCount = 0;
            }
            createIvy(predictionHit);
        }
        else if(predictionHit.transform.gameObject.tag == "Grappleable")
        {
            audioManager.PlayAudio("connectVine");
            // deactivate active grapple
            playerMovement.swinging = true;
            

            swingPoint = predictionHit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;

            if (predictionHit.transform.parent.GetComponent<FloatingPlatform>() != null)
            {
                joint.connectedBody = predictionHit.rigidbody;
                moving = true;
                playerMovement.moveSpeed = swingSpeed * 0.5f;
            }
            else {
                joint.connectedAnchor = swingPoint;
                playerMovement.moveSpeed = swingSpeed;
            }
            float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

            // the distance grapple will try to keep from grapple point. 
            joint.maxDistance = distanceFromPoint * 0.5f;
            joint.minDistance = distanceFromPoint * 0.35f;

            // customize values as you like
            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;

            lr.positionCount = 2;
            currentGrapplePosition = gunTip.position;
        }
    }
    private void OdmGearMovement()
    {
        Vector2 VerHor = VerHor_input.GetAxis(SteamVR_Input_Sources.LeftHand);

        int verticalInput = (VerHor.y >= 0.5f) ? 1 : 0;
        verticalInput = (VerHor.y <= -0.5f) ? -1 : verticalInput;
        int horizontalInput = (VerHor.x >= 0.5f) ? 1 : 0;
        horizontalInput = (VerHor.x <= -0.5f) ? -1 : horizontalInput;

       


        // right
        if (horizontalInput == 1 ) rb.AddForce(cam.right * horizontalThrustForce * Time.deltaTime);
        // left
        if (horizontalInput == -1) rb.AddForce(-cam.right * horizontalThrustForce * Time.deltaTime);

        // forward
        if (verticalInput == 1) rb.AddForce(cam.forward * horizontalThrustForce * Time.deltaTime);

        // shorten cable
        if (shortenRopeVR.GetState(SteamVR_Input_Sources.LeftHand))
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.5f;
            joint.minDistance = distanceFromPoint * 0.35f;
        }
        // extend cable
        if (verticalInput == -1)
        {
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = extendedDistanceFromPoint * 0.5f;
            joint.minDistance = extendedDistanceFromPoint * 0.35f;
        }
    }

    void StopGrapple()
  {
        moving = false;
        playerMovement.swinging = false;
        playerMovement.moveSpeed = o_MoveSpeed;
        lr.positionCount = 0;
        Destroy(joint);
  }
  void DrawRope()
  {
      //If not grappling, don't draw rope
      if (!joint) return;
      if (moving) swingPoint = predictionHit.transform.position;
      currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

      lr.SetPosition(0, gunTip.position);
      lr.SetPosition(1, currentGrapplePosition);
  }

    public void createIvy(RaycastHit hit)
    {
        Vector3 tangent = findTangentFromArbitraryNormal(hit.normal);           //����u
        GameObject ivy = new GameObject("Ivy " + ivyCount);
 
        for (int i = 0; i < branches; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(360 / branches * i + Random.Range(0, 360 / branches), hit.normal) * tangent;             //�u�۪���normal��ͦ�����K��V
            List<IvyNode> nodes = createBranch(maxPointsForBranch, hit.point, hit.normal, dir);
            GameObject branch = new GameObject("Branch " + i);
            Branch b = branch.AddComponent<Branch>();
            // if (!wantBlossoms) {
            b.init(nodes, branchRadius, branchMaterial);
            //  } else {
            //      b.init(nodes, branchRadius, branchMaterial, leafMaterial, leafPrefab, flowerMaterial, flowerPrefab, i == 0);
            //  }
            branch.transform.SetParent(ivy.transform);
            branch.layer = ivy.layer;
        }
        BoxCollider collider = ivy.AddComponent<BoxCollider>();

        Vector3 Hn = new Vector3 (Mathf.Abs(hit.normal.normalized.x), Mathf.Abs(hit.normal.normalized.y), Mathf.Abs(hit.normal.normalized.z));
        Vector3 boxsize = (Vector3.one - Hn) * 0.7f + Hn* 0.05f;
        ivy.transform.SetParent(IvyObject.transform);
        ivy.transform.tag = "Grappleable";

        collider.size = boxsize;
        collider.center = hit.point + hit.normal.normalized * 0.1f;
        ivyCount++;
    }

    Vector3 findTangentFromArbitraryNormal(Vector3 normal)
    {
        Vector3 t1 = Vector3.Cross(normal, Vector3.forward);
        Vector3 t2 = Vector3.Cross(normal, Vector3.up);
        if (t1.magnitude > t2.magnitude)
        {
            return t1;
        }
        return t2;
    }

    List<IvyNode> createBranch(int count, Vector3 pos, Vector3 normal, Vector3 dir)
    {

        if (count == maxPointsForBranch)
        {
            IvyNode rootNode = new IvyNode(pos, normal);
            return new List<IvyNode> { rootNode }.join(createBranch(count - 1, pos, normal, dir));
        }
        else if (count < maxPointsForBranch && count > 0)
        {

            if (count % 2 == 0)
            {
                dir = Quaternion.AngleAxis(Random.Range(-20.0f, 20.0f), normal) * dir;          //�Y�������I�A�b�쥻����V�[�W�H��������
            }

            RaycastHit hit;                                                             //�M����I
            Ray ray = new Ray(pos, normal);
            Vector3 p1 = pos + normal * segmentLength;

            if (Physics.Raycast(ray, out hit, segmentLength))                           
            {
                p1 = hit.point;
            }
            ray = new Ray(p1, dir);

            if (Physics.Raycast(ray, out hit, segmentLength))                           
            {
                Vector3 p2 = hit.point;
                IvyNode p2Node = new IvyNode(p2, -dir);                     //IvyNode(Vector3 position, Vector3 normal)
                return new List<IvyNode> { p2Node }.join(createBranch(count - 1, p2, -dir, normal));            //�O�����I�ñ���M��U�@���I
            }
            else                                                        //�|���I�쥭��
            {
                Vector3 p2 = p1 + dir * segmentLength;
                ray = new Ray(applyCorrection(p2, normal), -normal);        //�L��
                if (Physics.Raycast(ray, out hit, segmentLength))
                {
                    Vector3 p3 = hit.point;
                    IvyNode p3Node = new IvyNode(p3, normal);           

                    if (isOccluded(p3, pos, normal))                //�P�_�s�`�I����m�O�_�Q�B��
                    {
                        Vector3 middle = calculateMiddlePoint(p3, pos, (normal + dir) / 2);

                        Vector3 m0 = (pos + middle) / 2;
                        Vector3 m1 = (p3 + middle) / 2;

                        IvyNode m0Node = new IvyNode(m0, normal);
                        IvyNode m1Node = new IvyNode(m1, normal);

                        return new List<IvyNode> { m0Node, m1Node, p3Node }.join(createBranch(count - 3, p3, normal, dir));         //�O���W�[���I
                    }

                    return new List<IvyNode> { p3Node }.join(createBranch(count - 1, p3, normal, dir));         //�O�����I
                }
                else
                {
                    Vector3 p3 = p2 - normal * segmentLength;
                    ray = new Ray(applyCorrection(p3, normal), -normal);

                    if (Physics.Raycast(ray, out hit, segmentLength))
                    {
                        Vector3 p4 = hit.point;
                        IvyNode p4Node = new IvyNode(p4, normal);

                        if (isOccluded(p4, pos, normal))
                        {
                            Vector3 middle = calculateMiddlePoint(p4, pos, (normal + dir) / 2);
                            Vector3 m0 = (pos + middle) / 2;
                            Vector3 m1 = (p4 + middle) / 2;

                            IvyNode m0Node = new IvyNode(m0, normal);
                            IvyNode m1Node = new IvyNode(m1, normal);

                            return new List<IvyNode> { m0Node, m1Node, p4Node }.join(createBranch(count - 3, p4, normal, dir));
                        }

                        return new List<IvyNode> { p4Node }.join(createBranch(count - 1, p4, normal, dir));
                    }
                    else
                    {
                        Vector3 p4 = p3 - normal * segmentLength;
                        IvyNode p4Node = new IvyNode(p4, dir);

                        if (isOccluded(p4, pos, normal))
                        {
                            Vector3 middle = calculateMiddlePoint(p4, pos, (normal + dir) / 2);

                            Vector3 m0 = (pos + middle) / 2;
                            Vector3 m1 = (p4 + middle) / 2;

                            IvyNode m0Node = new IvyNode(m0, dir);
                            IvyNode m1Node = new IvyNode(m1, dir);

                            return new List<IvyNode> { m0Node, m1Node, p4Node }.join(createBranch(count - 3, p4, dir, -normal));
                        }
                        return new List<IvyNode> { p4Node }.join(createBranch(count - 1, p4, dir, -normal));
                    }
                }
            }

        }
        return null;
    }

    Vector3 applyCorrection(Vector3 p, Vector3 normal)
    {
        return p + normal * 0.01f;
    }
    bool isOccluded(Vector3 from, Vector3 to, Vector3 normal)
    {
        return isOccluded(applyCorrection(from, normal), applyCorrection(to, normal));
    }
    bool isOccluded(Vector3 from, Vector3 to)
    {
        Ray ray = new Ray(from, (to - from) / (to - from).magnitude);
        return Physics.Raycast(ray, (to - from).magnitude);
    }
    Vector3 calculateMiddlePoint(Vector3 p0, Vector3 p1, Vector3 normal)
    {
        Vector3 middle = (p0 + p1) / 2;
        var h = p0 - p1;
        var distance = h.magnitude;
        var dir = h / distance;
        return middle + normal * distance;
    }
    void Clear()
    {
        Destroy(IvyObject.transform.GetChild(0).gameObject); 
      //  MeshManager.instance.combineAll();
      //  foreach (Transform t in IvyParent.transform)
      //  {
      //      Destroy(t.gameObject);
      //  }
    }

    //   Vector3 calculateTangent(Vector3 p0, Vector3 p1, Vector3 normal) {
    //       var heading = p1 - p0;
    //       var distance = heading.magnitude;
    //       var direction = heading / distance;
    //       return Vector3.Cross(normal, direction).normalized;
    //   }


}