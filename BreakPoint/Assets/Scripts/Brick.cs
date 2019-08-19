using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class Brick : MonoBehaviour {

    SpriteRenderer spriteRenderer;

    public List<Sprite> sprites;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public Brick(int state)
    {
        this.state = state;
        value = 100;
    }
    public int value = -1;
    public int state = -1;

    public void Hit()
    {
        if(state == 1)
        {
            Camera.main.GetComponent<CameraShake>().Shake();
            state = 0;
            spriteRenderer.sprite = sprites[0];
        }
        else if(state == 0)
        {
            Destroy(this.gameObject);
        }
    }
}
