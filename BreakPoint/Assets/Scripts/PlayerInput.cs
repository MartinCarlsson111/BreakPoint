using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class PlayerInput : MonoBehaviour
{
    Flipper[] flipper;
    public float movementSpeed = 1.0f;
    Camera playerCamera;
    Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        flipper = GetComponentsInChildren<Flipper>();

        playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {

        float xPos = playerCamera.ScreenToWorldPoint(Input.mousePosition).x;

        rb.velocity = new float2((xPos - transform.position.x) * 10, 0);
        
       //transform.position = new Vector3(xPos, transform.position.y, transform.position.z);
        /*
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(new Vector3(-movementSpeed * Time.deltaTime, 0));
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(new Vector3(movementSpeed * Time.deltaTime, 0));
        }*/

        if (Input.GetKeyDown(KeyCode.Space) && flipper != null) 
        {
            foreach(var flip in flipper)
            {
                flip.Flip();
            }
        }
        else
        {
            foreach(var flip in flipper)
            {
                flip.StopFlipping();
            }
        }
    }
}
