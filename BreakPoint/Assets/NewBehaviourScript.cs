using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    const float maxRot = 90.0f;
    const float minRot = 0.0f;

    const float rotationSpeed = 1.0f;

    private IEnumerator Rotate()
    {
        this.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 0, 1), -rotationSpeed);
        //if(this.transform.rotation.z >= maxRot)
        {
            StopCoroutine("Rotate");
        }
        yield return new WaitForSeconds(0.016f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine("Rotate");
        }
    }



}
