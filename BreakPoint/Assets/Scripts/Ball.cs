using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Ball : MonoBehaviour
{
    Rigidbody2D rb;
    public float speed = 1.0f;
    // Update is called once per frame
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    void FixedUpdate()
    {
        float2 velocity = rb.velocity.normalized;

        velocity *= speed * UnityEngine.Random.Range(0.85f, 1.0f);
        rb.velocity = velocity + new float2(UnityEngine.Random.Range(0.00f, 0.01f));


    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        Brick brick;
        if(collision.collider.TryGetComponent<Brick>(out brick))
        {
            brick.Hit();
        }
    }
}
