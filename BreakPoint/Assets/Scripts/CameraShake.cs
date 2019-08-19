using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class CameraShake : MonoBehaviour
{
    new Camera camera;

    public float time = 0.3f;
    public float3 originalPos;
    public Quaternion originalRot;


    private void Awake()
    {
        camera = this.GetComponent<Camera>();
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    public void Shake()
    {
        if(camera == null)
        {
            camera = Camera.main;
        }

        StopCoroutine("Shake");
        StartCoroutine("Shaking", time);
    }


    private IEnumerator Shaking(float time)
    {
        float accu = 0;
        float a = -1;
        float s = UnityEngine.Random.Range(0.01f, 0.01f);
        Quaternion rot = Quaternion.Euler(0, 0, math.radians(10f));

        while (accu < time)
        {
            accu += Time.deltaTime;
            camera.transform.Translate(new float3(s * a, s * a, 0));
            if (a < 0)
            {
                camera.transform.Rotate(rot.eulerAngles);
            }
            else
            {
                camera.transform.Rotate(Quaternion.Inverse(rot).eulerAngles);
            }
            a = -a;

            if (a > 0)
            {
                s = UnityEngine.Random.Range(0.05f, 0.15f);

            }
            yield return new WaitForSeconds(0.016f * 2);
        }
        ResetCamera();
    }
    void ResetCamera()
    {
        transform.SetPositionAndRotation(originalPos, originalRot);
    }
}
