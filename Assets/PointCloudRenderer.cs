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
using PointCloudExporter;

using PC2;
using PointCloudExporter;
public class PointCloudRenderer : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/camera/camera/depth/color/points";

    int i = 0;
    [Header("MAKE SURE THESE LISTS ARE MINIMISED OR EDITOR WILL CRASH")]

    bool isMessageReceived = false;
    private const int verticesMax = 64998;

    private Mesh mesh;
    private MeshInfos mesh_infos;
    private byte[] byteArray;
    
    public bool UseNormals = true;    
    private int RGB_OFFSET = 32; 
    private const int NORMALS_OFFSET = 16;
    private int size;
    private int width;
    private int height;
    private int row_step;
    private int point_step;

    public GameObject PCL;

    [Range(0f, 2f)]
    public float TriangleSize = 0.3f;

    public int GetNearestPowerOfTwo(float x)
    {
        return (int)Mathf.Pow(2f, Mathf.Ceil(Mathf.Log(x) / Mathf.Log(2f)));
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointCloud2Msg>(topicName, StorePointCloud);
        mesh_infos = new MeshInfos();
        mesh = new Mesh();
        if(UseNormals == false)
        {
            RGB_OFFSET = 16;
        }
    }
    void Update()
    {
        if (isMessageReceived)
        {
            ParsePointCloud();
            UpdateMesh();
            isMessageReceived = false;
        }

    }
    protected void StorePointCloud(PointCloud2Msg message)
    {
        i++;
        byteArray = new byte[size];
        byteArray = message.data;
        width = (int)message.width;
        height = (int)message.height;

        row_step = (int)message.row_step;
        size = row_step * height;
        point_step = (int)message.point_step;
        size = size / point_step;
        isMessageReceived = true;
    }
    void ParsePointCloud()
    {

        mesh_infos = new MeshInfos();
        mesh_infos.vertexCount = 0;
        mesh_infos.vertices = new Vector3[size];
        mesh_infos.colors = new Color[size];
        mesh_infos.normals = new Vector3[size];
        mesh_infos.vertexCount = size;
        int x_posi;
        int y_posi;
        int z_posi;
        float x;
        float y;
        float z;
        int rgb_posi;
        int rgb_max = 255;
        float r;
        float g;
        float b;
        for (int n = 0; n < size-1; n++)
        {
            x_posi = n * point_step + 0;
            y_posi = n * point_step + 4;
            z_posi = n * point_step + 8;
            x = BitConverter.ToSingle(byteArray, x_posi);
            y = BitConverter.ToSingle(byteArray, y_posi);
            z = BitConverter.ToSingle(byteArray, z_posi);
            rgb_posi = n * point_step + RGB_OFFSET;
            b = byteArray[rgb_posi + 0];
            g = byteArray[rgb_posi + 1];
            r = byteArray[rgb_posi + 2];
            r = r / rgb_max;
            g = g / rgb_max;
            b = b / rgb_max;
            mesh_infos.vertices[n] = new Vector3(x, z, y);
            mesh_infos.colors[n] = new Color(r, g, b);
            if(UseNormals == true){
                int normal_x_posi = n * point_step + NORMALS_OFFSET;
                int normal_y_posi = n * point_step + NORMALS_OFFSET + 4;
                int normal_z_posi = n * point_step + NORMALS_OFFSET + 8;
                float normal_x = BitConverter.ToSingle(byteArray, normal_x_posi);
                float normal_y = BitConverter.ToSingle(byteArray, normal_y_posi);
                float normal_z = BitConverter.ToSingle(byteArray, normal_z_posi);
                mesh_infos.normals[n] = new Vector3(normal_x, normal_z, normal_y);
            }else{
                mesh_infos.normals[n] = new Vector3(1f, 1f, 1f);
            }
        }
        mesh_infos.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
    }
    void UpdateMesh()
    {
        Generate(mesh_infos, MeshTopology.Points);
    }

    public void Generate(MeshInfos meshInfos, MeshTopology topology)
    {
        int vertexCount = meshInfos.vertexCount;
        int meshCount = (int)Mathf.Ceil(vertexCount / (float)verticesMax);
        int index = 0;
        int meshIndex = 0;
        int vertexIndex = 0;
        int resolution = GetNearestPowerOfTwo(Mathf.Sqrt(vertexCount));
        int count = verticesMax;
        if (vertexCount <= verticesMax)
        {
            count = vertexCount;
        }
        else if (vertexCount > verticesMax && meshCount == meshIndex + 1)
        {
            count = vertexCount % verticesMax;
        }
        int[] subIndices = new int[count];
        for (int i = 0; i < count; ++i)
        {
            subIndices[i] = i;
        }
        mesh.Clear();
        mesh.vertices = meshInfos.vertices;
        mesh.normals = meshInfos.normals;
        mesh.colors = meshInfos.colors;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        mesh.SetIndices(subIndices, topology, 0);
        Vector2[] uvs2 = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs2.Length; ++i)
        {
            float x = vertexIndex % resolution;
            float y = Mathf.Floor(vertexIndex / (float)resolution);
            uvs2[i] = new Vector2(x, y) / (float)resolution;
            ++vertexIndex;
        }
        mesh.uv2 = uvs2;

        PCL.GetComponent<MeshFilter>().mesh = mesh;
        PCL.GetComponent<MeshRenderer>().material.SetFloat("_Size", TriangleSize);
    }
}



