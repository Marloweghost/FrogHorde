using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObject : MonoBehaviour
{
    public float density = 1f;

    private Rigidbody _rb;
    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass = transform.localScale.x * transform.localScale.y * transform.localScale.z * density;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
