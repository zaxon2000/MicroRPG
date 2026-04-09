using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHookHandler : MonoBehaviour
{

    
    public enum HookTravelMode { Instant, Straight, Gravity };
    public enum RappelMode { None, Force, Controlled};


    //Define how the hook travels
    [Tooltip("Instant - Imediately Hooks to target position\nStraigth - Throws hook in a line\nGravity - Hook is affected by gravity")]
    public HookTravelMode hookmode = HookTravelMode.Instant;

    //defines how the player travels
    [Tooltip("None - Can't Rappel\nForce - Progressively Travels to Location\nControlled - Go further or closer")]
    public RappelMode rappelMode = RappelMode.Controlled;

    //what function is executed to throw hook
    delegate void ThrowDeleg(Vector3 target);
    ThrowDeleg ThrowDelegate;

    // what function is executed to travel player
    delegate void RappelDeleg(GameObject hook, int dir, float rappelSpeed);
    RappelDeleg RappelDelegate;

    
    [Tooltip("Can it hook anywhere or it needs an object to hook into? Only Used if hookMode = Instant)")]
    public bool hookNeedsCollider = true;


    [Tooltip("What Object Can it Hook to")]
    public float maxDistance = 4f;

    //hook prefab
    public GameObject hookFab;

    [Tooltip("Player Object")]
    public Transform player;

    [Tooltip("How quickly the hook travles")]
    public float hookSpeed;

    [Tooltip("How quickly is the rappel")]
    public float reelSpeed;

    [Tooltip("true - Constant Distance\nfalse - Max distance or shorter")]
    public bool maxFixedDistance;

    //current existent hook
    GameObject curHook;


    //is the player rapelling
    [HideInInspector()]
    public bool playerTraveling = false;

    //player rb
    Rigidbody2D rb;

    //position of player when he last threw hook
    Vector3 playerThrowPos;

    //is hook attached to a wall
    bool hookHooked = false;
    public bool HookHooked { get => hookHooked; set => hookHooked = value; }


    //rope
    LineRenderer lr;

    

    private void Awake()
    {
        //pick rappel mode
        switch (rappelMode)
        {
            case RappelMode.None:
                RappelDelegate = RappelNone;
                break;
            case RappelMode.Controlled:
                RappelDelegate = RappelReel;
                break;
           case RappelMode.Force:
                RappelDelegate = RappelForce;
                break;     
        }

        //pick hook mode
        switch (hookmode)
        {
            case HookTravelMode.Instant:
                ThrowDelegate = ThrowHookInstant;
                break;
            case HookTravelMode.Straight:
                ThrowDelegate = ThrowHookStraigth;
                break;
            case HookTravelMode.Gravity:
                ThrowDelegate = ThrowHookGravity;
                break;
        }


        //find player on the scene if not added
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (player == null)
        {
            Debug.LogWarning("Assign player to the script");
        }

        //fetch rb reference
        rb = player.GetComponent<Rigidbody2D>();

        //fetch line renderer
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;
    }



    // Public function used to throw hook
    public void Throw(Vector3 target)
    {
        ThrowDelegate(target);
    }

    // Public function used to Reeling player
    public void Reel(int dir)
    {

        //only allow if player attached
        if (curHook == null)
            return;

        //set player as travelling
        playerTraveling = true;


        RappelDelegate(curHook, dir, reelSpeed);
    }


    //Spawn hook and rope
    public GameObject CreateHook(Vector3 target, Quaternion rot)
    {
        GameObject hook = Instantiate(hookFab, target, rot);
        hook.GetComponent<Hook>().grapplehandler = this;
        curHook = hook;

        lr.enabled = true;
        lr.SetPosition(0, player.transform.position);
        lr.SetPosition(1, curHook.transform.GetChild(0).position);


        return hook;
    }

    // Public function to stop Reeling
    void DeactivateReel()
    {
        //come back to normal
        playerTraveling = false;
        rb.gravityScale = 1;
        
    }
    


    // Public function to remove hook
    public void RemoveHook()
    {

        Destroy(curHook);
        DeactivateReel();

        //remove hook
        hookHooked = false;

        lr.enabled = false;
    }


    /// <summary>
    /// Ataches the hook to an object or to nothing, and create rope
    /// </summary>
    /// <param name="hookpoint">Point in space where object will be attached</param>
    /// <param name="attached">rigidbody2D wher ethe hook will be attached, can be null if does not need object to attach</param>
    /// <returns></returns>
    public GameObject AttachHook(Vector2 hookpoint, Rigidbody2D attached)
    {
        Hook hoook = curHook.GetComponent<Hook>();

        //useto lock 
        hoook.fixedJoint.autoConfigureConnectedAnchor = false;

        hoook.fixedJoint.enabled = true;

        //hook.GetComponent<Rigidbody2D>().simulated = false;
        DistanceJoint2D distJ = curHook.GetComponent<DistanceJoint2D>();

        distJ.enabled = true;
        distJ.connectedBody = player.GetComponent<Rigidbody2D>();
        distJ.maxDistanceOnly = !maxFixedDistance;

        //hook.transform.SetParent(hit.collider.transform);
        hoook.fixedJoint.connectedAnchor = hookpoint;
        hoook.fixedJoint.autoConfigureConnectedAnchor = true;

        //connect null
        hoook.fixedJoint.connectedBody = attached;

        hoook.fixedJoint.autoConfigureConnectedAnchor = false;

        hookHooked = true;

        return curHook;
    }





    void Update()
    {


        //check maxdist and remove hook if travelled more than that
        if (curHook != null && Vector2.Distance(playerThrowPos, curHook.transform.position) > maxDistance && hookHooked == false)
        {
            RemoveHook();
        }

        //if player is reeling and force, update travel distance, in case hook is attached to moving object
        if (rappelMode == RappelMode.Force && playerTraveling)
        {
            rb.linearVelocity = reelSpeed * (curHook.transform.position - rb.transform.position).normalized;
        }


        //update rope
        if (curHook != null)
        {

            //update positions
            lr.SetPosition(0, player.transform.position);
            lr.SetPosition(1, curHook.transform.GetChild(0).position);

            //update school
            lr.material.mainTextureScale = new Vector2(Vector3.Distance(player.transform.position, curHook.transform.position), 1);
            lr.material.mainTextureOffset = new Vector2(-Vector3.Distance(player.transform.position, curHook.transform.position), 1);
        }

    }

    #region ThrowFunctions

    /// <summary>
    /// Casts a ray and creates hook on first impacted place
    /// If no collider is needed  it simply create it on that spot
    /// </summary>
    /// <param name="target"></param>
    void ThrowHookInstant(Vector3 target)
    {
        Physics2D.queriesStartInColliders = false;

        Vector2 hookpoint=Vector2.zero;
        Rigidbody2D rbAttach = null;
        RaycastHit2D hit = new RaycastHit2D();     

        //if it can hook anywhere just set the target as the point to hook
        if (hookNeedsCollider == false)
        {
            hookpoint = target;

        }else
        {
            hit = Physics2D.Raycast(player.position, target - player.position, maxDistance);


            if (hit.collider == null)
            {
                Debug.Log("No Object to Grapple to. Or too far");
                return;
            }
            else
            {

                Debug.Log(hit.collider.name);
                Debug.Log(hit.point);
                hookpoint = hit.point;
                rbAttach = hit.collider.attachedRigidbody;
            }
        }
        

       
            
        
                   


        //remove previous hook
        if (curHook != null)
        {
            RemoveHook();
        }

        //instantiate hook and set roations
        GameObject hook =  CreateHook(hookpoint, Look2D(target - player.transform.position));
        
        //connect hook to object and activate hook
        AttachHook(hookpoint, rbAttach);

        


    }


    /// <summary>
    /// Throw hook in  a straight line
    /// </summary>
    /// <param name="target"></param>
    public void ThrowHookStraigth(Vector3 target)
    {
        Physics2D.queriesStartInColliders = false;
      
        //remove previous hook
        if (curHook != null)
        {
            RemoveHook();
        }

        //throw hook
        GameObject hook =  CreateHook(player.transform.position,Quaternion.identity);

        //rotate hook
        Vector3 diff = target - player.transform.position;
        diff.Normalize();
        hook.transform.rotation = Look2D(diff);

        //make hook traveable
        Rigidbody2D hookRB = hook.GetComponent<Rigidbody2D>();
        hookRB.bodyType = RigidbodyType2D.Kinematic;

        //set hook velocity
        hookRB.linearVelocity = hookSpeed * (target - player.transform.position).normalized;

        //save throwing position
        playerThrowPos = player.transform.position;

    }

    /// <summary>
    /// Throw hook, gravity is applied to it
    /// </summary>
    /// <param name="target"></param>
    public void ThrowHookGravity(Vector3 target)
    {
        Physics2D.queriesStartInColliders = false;

        //remove previous hook
        if (curHook != null)
        {
            RemoveHook();
        }

        //throw hook
        GameObject hook = CreateHook(player.transform.position,Quaternion.identity);

        //rotate hook
        Vector3 dir = target - player.transform.position;
        hook.transform.rotation = Look2D(dir);


        //add velocity
        Rigidbody2D hookRB = hook.GetComponent<Rigidbody2D>();
        hookRB.linearVelocity = hookSpeed * (target - player.transform.position).normalized;

        //save throwing position
        playerThrowPos = player.transform.position;

    }


    /// <summary>
    /// Rotate in a direction
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    Quaternion Look2D(Vector3 dir)
    {
        dir.Normalize();

        float rot_z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, rot_z - 90);

    }
    #endregion


    #region RappelFunctions


    //dont rappel
    public void RappelNone(GameObject hook, int dir, float rappelSpeed)
    {

    }



    /// <summary>
    /// Controlled Rappel, up nd down
    /// </summary>
    /// <param name="hook">hook gameobject</param>
    /// <param name="dir">1,-1 - go up or down</param>
    /// <param name="rappelSpeed">how quick</param>
    public void RappelReel(GameObject hook, int dir, float rappelSpeed)
    {
        //fetch joint
        DistanceJoint2D distJoint = hook.GetComponent<DistanceJoint2D>();

        //save state
        bool enforcedDist = distJoint.maxDistanceOnly;

        //increase or decrease rope
        distJoint.distance += dir * rappelSpeed;

        
        distJoint.maxDistanceOnly = enforcedDist;
    }

    // Rappel Force, Goes

    /// <summary>
    /// Get pulled into the hook  position
    /// </summary>
    /// <param name="hook">hook gameobject</param>
    /// <param name="dir">1,-1 - go up or down</param>
    /// <param name="rappelSpeed">how quick</param>
    public void RappelForce(GameObject hook, int dir, float rappelSpeed)
    {

        DistanceJoint2D distJoint = hook.GetComponent<DistanceJoint2D>();

        //save state
        bool enforcedDist = distJoint.maxDistanceOnly;


        Rigidbody2D playerRB = distJoint.connectedBody;

        //make it float and move
        playerRB.gravityScale = 0;  
        playerRB.linearVelocity = rappelSpeed * (hook.transform.position - playerRB.transform.position).normalized;

        
        distJoint.enabled = false;

        playerRB.useFullKinematicContacts = true;


        distJoint.maxDistanceOnly = enforcedDist;
    }
    #endregion

}
