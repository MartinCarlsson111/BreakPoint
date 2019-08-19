using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public static CameraScript instance;
    [System.NonSerialized] public CameraShake shake;

    private void Awake()
    {
  
        shake = GetComponent<CameraShake>();
    }

    public static CameraScript GetInstance()
    {
        if(instance == null) instance = new CameraScript();

        return instance; 
    }

    public void Shake()
    {
        if(shake == null)
        {
            shake = GetComponent<CameraShake>();
        }
        shake.Shake();
    }

}
