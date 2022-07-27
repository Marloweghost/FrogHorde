using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public bool spawn = false;
    public GameObject cubePrefab;
    public float launchForce = 50f;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("SpawnCube", 0f, 2f);
    }

    // Update is called once per frame
    void Update()
    {
        if (spawn == true)
        {
            SpawnCube();
            spawn = false;
        } 
    }

    void SpawnCube()
    {
        GameObject cube = Instantiate(cubePrefab, transform.position, Quaternion.identity);
        float randomScaleAxis = Random.Range(0.8f, 1.2f);
        cube.transform.localScale = new Vector3(randomScaleAxis, randomScaleAxis, randomScaleAxis);
        cube.GetComponent<Rigidbody>().AddForce(transform.forward * launchForce, ForceMode.Impulse);
    }
}
