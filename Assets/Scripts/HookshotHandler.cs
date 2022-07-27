using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookshotHandler : MonoBehaviour
{
    public bool colliderFound = false;
    public string colliderLayerName;
    public Collider colliderObject = null;
    public Vector3 collisionPoint;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("DynEnv"))
        {
            colliderFound = true;
            colliderLayerName = "DynEnv";
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            colliderFound = true;
            colliderLayerName = "Ground";
            collisionPoint = other.ClosestPoint(transform.position);
        }

        colliderObject = other;
    }

    public void HookshotReset()
    {
        colliderFound = false;
        colliderLayerName = "";
        colliderObject = null;
        collisionPoint = Vector3.zero;
    }
}
