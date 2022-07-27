using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashingCube : MonoBehaviour
{
    public float destroyAfter = 5f;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("DestroyObject", destroyAfter);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }
}
