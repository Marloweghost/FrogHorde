using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyEffect : MonoBehaviour
{
    public float Intensity = 1f;
    public float Mass = 1f;
    public float Stiffness = 1f;
    public float Damping = 0.75f;

    private Mesh originalMesh, cloneMesh;
    private MeshRenderer meshRenderer;
    private JellyVertex[] jv;
    private Vector3[] vertexArray;

    void Start()
    {
        originalMesh = GetComponent<MeshFilter>().sharedMesh;
        cloneMesh = Instantiate(originalMesh);
        GetComponent<MeshFilter>().sharedMesh = cloneMesh;
        meshRenderer = GetComponent<MeshRenderer>();

        jv = new JellyVertex[cloneMesh.vertices.Length];
        for (int i = 0; i < cloneMesh.vertices.Length; i++)
        {
            jv[i] = new JellyVertex(i, transform.TransformPoint(cloneMesh.vertices[i]));
        }
    }

    void FixedUpdate()
    {
        vertexArray = originalMesh.vertices;
        for (int i = 0; i < jv.Length; i++)
        {
            Vector3 target = transform.TransformPoint(vertexArray[jv[i].ID]);
            float intensity = (1 - (meshRenderer.bounds.max.y - target.y) / meshRenderer.bounds.size.y) * Intensity;
            jv[i].Shake(target, Mass, Stiffness, Damping);
            target = transform.InverseTransformPoint(jv[i].position);
            vertexArray[jv[i].ID] = Vector3.Lerp(vertexArray[jv[i].ID], target, intensity);
        }
        cloneMesh.vertices = vertexArray;
    }

    void Update()
    {

    }

    public class JellyVertex
    {
        public int ID;
        public Vector3 position, velocity, force;

        public JellyVertex(int _id, Vector3 _pos)
        {
            ID = _id;
            position = _pos;
        }

        public void Shake(Vector3 target, float m, float s, float d)
        {
            force = (target - position) * s;
            velocity = (velocity + force / m) * d;
            position += velocity;
            if ((velocity + force / m).magnitude < 0.001f)
                position = target;
        }
    }


}
