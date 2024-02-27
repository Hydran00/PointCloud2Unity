using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using UnityEngine.UI;
using System.Threading;
public class PC2SingleMesh : MonoBehaviour
{
    public enum GeometryShaderShape { Point, Triangle };
    
    [Range(0.01f, 0.2f)]
    public float PointSize = 0.1f;

    [SerializeField]
    public GeometryShaderShape geometryShaderShape;
    public PointCloud2Decoder PC2Decoder;
    // maximum number of vertices drawable per mesh
    private Mesh mesh;
    private int chunks_num = 0;


    public int GetNearestPowerOfTwo(float x)
    {
        return (int)Mathf.Pow(2f, Mathf.Ceil(Mathf.Log(x) / Mathf.Log(2f)));
    }

    void Start()
    {
        mesh = new Mesh{
            // allow more than 65535 vertices
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
    }
    void Update()
    {
        if(geometryShaderShape == GeometryShaderShape.Point)
        {
            this.GetComponent<MeshRenderer>().material =
             new Material(Shader.Find("Custom/PointMeshShader"));
        }else{
            this.GetComponent<MeshRenderer>().material =
             new Material(Shader.Find("Custom/TriangleMeshShader"));
        }
        if (PC2Decoder.isMessageReceived)
        {
            PC2Decoder.ParsePointCloud();
            Generate();
            PC2Decoder.isMessageReceived = false;
        }
    }

    public void Generate()
    {

        int vertexCount = PC2Decoder.mesh_infos.vertexCount;
        int vertexIndex = 0;
        int resolution = GetNearestPowerOfTwo(Mathf.Sqrt(vertexCount));
        int[] subIndices = new int[PC2Decoder.mesh_infos.vertexCount];
        for (int i = 0; i < PC2Decoder.mesh_infos.vertexCount; ++i)
        {
            subIndices[i] = i;
        }
        mesh.Clear();
        mesh.vertices = PC2Decoder.mesh_infos.vertices;
        mesh.normals = PC2Decoder.mesh_infos.normals;
        mesh.colors = PC2Decoder.mesh_infos.colors;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        mesh.SetIndices(subIndices, MeshTopology.Points, 0);
        Vector2[] uvs2 = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs2.Length; ++i)
        {
            float x = vertexIndex % resolution;
            float y = Mathf.Floor(vertexIndex / (float)resolution);
            uvs2[i] = new Vector2(x, y) / (float)resolution;
            ++vertexIndex;
        }
        mesh.uv2 = uvs2;

        this.GetComponent<MeshFilter>().mesh = mesh;
        this.GetComponent<MeshRenderer>().material.SetFloat("_Size", PointSize);
    }
}



