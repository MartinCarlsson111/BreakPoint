using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flipper : MonoBehaviour
{

    public int extendedRotZ;
    public float speed;
    bool finishedExtending = false;
    bool finishedReset = true;

    public float extensionPause = .0f;

    Quaternion extendedRot;
    Quaternion originRot;
    // Update is called once per frame

    private IEnumerator extend;
    private IEnumerator reset;
    private void Start()
    {
        originRot = Quaternion.identity;
        extendedRot = Quaternion.Euler(0, 0, extendedRotZ);
        extend = Rotate(extendedRot, false);
        reset = Rotate(originRot, true);
    }
    void Update()
    {
        if(finishedExtending)
        {
            finishedExtending = false;
            StartCoroutine(reset);
            reset = Rotate(originRot, true);
        }

    }

    public void Flip()
    {
        if (finishedReset)
        {
            finishedReset = false;
            StartCoroutine(extend);
            extend = Rotate(extendedRot, false);
        }
    }

    public void StopFlipping()
    {
        //StopCoroutine("Rotate");
    }

    private IEnumerator Rotate(Quaternion quat, bool reset) { 
        float time = 0.0f;
        Quaternion startRot = this.transform.rotation;
        while(time <= 1.0f)
        {
            time += Time.deltaTime * speed;
            Quaternion slerp = Quaternion.Slerp(startRot, quat, time);
            this.transform.SetPositionAndRotation(this.transform.position, slerp);
            yield return null;
        }
        yield return new WaitForSeconds(extensionPause);
        if(reset)
        {
            finishedReset = true;
        }
        else
        {
            finishedExtending = true;
        }
        yield break;
    }

}
