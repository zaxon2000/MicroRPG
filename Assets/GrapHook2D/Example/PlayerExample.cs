using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerExample : MonoBehaviour
{
    //Sample Player Scripts, shows how to use the grapple hook with your own player

    
    //import the grappler addler
    public GrappleHookHandler grappleHandler;

    Rigidbody2D rb;

    [SerializeField]
    float movementSpeed=5;
    [SerializeField]
    float jumpForce = 100f;

    [SerializeField]
    float groundOffset = -1f;

    [SerializeField]
    float groundRadius = 0.2f;

    // Start is called before the first frame update
    void Awake()
    { 
        rb = GetComponent<Rigidbody2D>();

        if(grappleHandler ==null)
        {
            Debug.LogWarning("No Grapple Hook Handler Component, Please Add it on the scene");
        }
    }


    void Update()
    {
        //left and right on air
        if(Input.GetAxisRaw("Horizontal")!=0)
        {        
            rb.linearVelocity = new Vector2(Input.GetAxisRaw("Horizontal") * movementSpeed, rb.linearVelocity.y);

        //if not hooked, player stops moving when no key is pressed. This allows for gravity to work when object is indeed hooked
        }

        //detect if on floor
        bool grounded = Physics2D.OverlapCircle(transform.position - Vector3.down * groundOffset, groundRadius);


        if(grounded)
        {

            //jump
            if (Input.GetKeyDown(KeyCode.Space))
            {
                rb.AddForce(Vector2.up * jumpForce);
            }

            //left and right on floor
            if (grappleHandler.HookHooked == false)
            { 
             rb.linearVelocity = new Vector2(Input.GetAxisRaw("Horizontal") * movementSpeed, rb.linearVelocity.y);
            }

        }

        //Launch Hook example
        if (Input.GetMouseButtonDown(0))
        {

            //Calculate mouse position in the world
            var v3 = Input.mousePosition;
            v3.z = 10.0f;
            v3 = Camera.main.ScreenToWorldPoint(v3);
            Vector2 dir = v3 - transform.position;


            grappleHandler.Throw(v3);
        }

        //Detach Hook
        if (Input.GetMouseButtonDown(1))
        {
            grappleHandler.RemoveHook();
        }

        //Rappelling Down
        if (Input.GetKey(KeyCode.G))
        {
            grappleHandler.Reel(1);
        }
        //Rappel Up, If mode is rappelcontrolled
        else if (Input.GetKey(KeyCode.T))
        {
            //only works if reel mode is controlled
            grappleHandler.Reel(-1);
        }

    }

    
    private void OnTriggerEnter2D(Collider2D col)
    {
        //if collides with hook, remove hook
        if(col.tag == "Hook" && grappleHandler.playerTraveling)
        {

        grappleHandler.RemoveHook();
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if player is rappeling and hits object, stop hook
        if (grappleHandler.playerTraveling)
        {
            grappleHandler.RemoveHook();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(transform.position - Vector3.down * groundOffset, groundRadius);
    }
}
