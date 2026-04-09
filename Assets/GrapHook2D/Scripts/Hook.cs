using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hook : MonoBehaviour
{
    //hook rb
    Rigidbody2D rb;

    //grapling
    public GrappleHookHandler grapplehandler;

    public FixedJoint2D fixedJoint;
    
    
    
    

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if(transform.childCount<1)
        {
            Debug.LogWarning("Please Add origin point GameObject as child of the hook");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //when an object is hit attach hook to it

        if(collision.tag!= "Player")
        {        
            //hook it  
            rb.linearVelocity = Vector3.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
            grapplehandler.AttachHook(transform.position, collision.attachedRigidbody);
            
        }
    }
}
