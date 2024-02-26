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
// using PointCloudExporter;
public class PointCloudRenderer : MonoBehaviour
{
    public bool UseNormals = true;
    public bool GPU_instancing = true;
    public Mesh pointMesh;
    // public Camera CurrentCamera;
    bool faceCamera = true;
    public float normalOffset = 0f;
    [Range(0f, 0.08f)]
    public float globalScale = 0.1f;
    public Vector3 quaternionOffset;
    Vector3 scale = Vector3.one;

    public Material pointMaterial;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();

    MaterialPropertyBlock propertyBlock;
    /// <summary>
    /// //
    /// </summary>
    ROSConnection ros;
    public string topicName = "/camera/camera/depth/color/points";

    int i = 0;
    [Header("MAKE SURE THESE LISTS ARE MINIMISED OR EDITOR WILL CRASH")]

    bool isMessageReceived = false;
    private const int verticesMax = 65535;

    private Mesh mesh;
    private MeshInfos mesh_infos;
    private byte[] byteArray;

    private int RGB_OFFSET = 32;
    private const int NORMALS_OFFSET = 16;
    private int size;
    private int width;
    private int height;
    private int row_step;
    private int point_step;
    private Shader shader;
    private List<Material> mats;
    private Matrix4x4 matrix;
    private Vector4[] colors;
    public GameObject PCL;

    // [Range(0f, 2f)]
    private float TriangleSize = 0.3f;

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
        if (UseNormals == false)
        {
            RGB_OFFSET = 16;
        }
        shader = Shader.Find("Custom/PC2CPU");
        mats = new List<Material>();

    }
    void Update()
    {

        // update every fps interval

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
        for (int n = 0; n < size - 1; n++)
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
            if (UseNormals == true)
            {
                int normal_x_posi = n * point_step + NORMALS_OFFSET;
                int normal_y_posi = n * point_step + NORMALS_OFFSET + 4;
                int normal_z_posi = n * point_step + NORMALS_OFFSET + 8;
                float normal_x = BitConverter.ToSingle(byteArray, normal_x_posi);
                float normal_y = BitConverter.ToSingle(byteArray, normal_y_posi);
                float normal_z = BitConverter.ToSingle(byteArray, normal_z_posi);
                mesh_infos.normals[n] = new Vector3(normal_x, normal_z, normal_y);
            }
            else
            {
                mesh_infos.normals[n] = new Vector3(1f, 1f, 1f);
            }
        }
        mesh_infos.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
    }

    void UpdateMesh()
    {
        // Generate(mesh_infos, MeshTopology.Points);
        if (GPU_instancing)
        {
            GenerateGPUInstanced();
        }
        else
        {
            GenerateCPU();
        }
    }







    Matrix4x4 GetPointTransforms(int i)
    {
        Vector3 position = transform.position + transform.rotation * mesh_infos.vertices[i] +
                       transform.rotation * (mesh_infos.normals[i].normalized * normalOffset);

        Quaternion rotation = Quaternion.identity;
        // if (faceCamera)
        // {
        //     rotation = Quaternion.LookRotation(CurrentCamera.transform.position - position);
        // }
        // else
        // {
        //     rotation = transform.rotation * Quaternion.LookRotation(mesh_infos.normals[i]);
        // }
        return Matrix4x4.TRS(position, rotation, scale * globalScale);
    }

    void GenerateCPU()
    {
        Color color = new Color(1, 1, 1, 1);
        Material m = new Material(shader);
        Matrix4x4 matrix = new Matrix4x4();
        for (int i = 0; i < mesh_infos.vertexCount; i++)
        {
            matrices.Add(GetPointTransforms(i));

            // mats.Add(new Material(shader));
            // mats[i].SetColor("_Color", mesh_infos.colors[i]);
            m.SetColor("_Color", color);
            Graphics.DrawMesh(pointMesh, matrices[i], m, 0);
        }
        // mats.Clear();
        matrices.Clear();
    }

void GenerateGPUInstanced()
{
    Debug.Log("Generating GPU Instanced");

    int maxInstancesPerDrawCall = 1023;
    int startIndex = 0;
    int endIndex = Mathf.Min(maxInstancesPerDrawCall, mesh_infos.vertexCount);
    int pointCount = 0;

    int chunks_num = (int)Mathf.Ceil(mesh_infos.vertexCount / (float)maxInstancesPerDrawCall);

    // Preallocate arrays and lists outside the loop
    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock(); // Create a property block for each instance
    Vector4[] colors = new Vector4[maxInstancesPerDrawCall];
    List<Matrix4x4> matrices = new List<Matrix4x4>(maxInstancesPerDrawCall);
    Matrix4x4 matrix = new Matrix4x4();
    Vector4 c = new Vector4(1, 1, 1, 1);
    
    while (pointCount < mesh_infos.vertexCount)
    {
        matrices.Clear(); // Clear matrices list for reuse

        // Generate a single chunk of points
        for (int i = startIndex; i < endIndex; i++)
        {
            matrices.Add(Matrix4x4.TRS(transform.position + transform.rotation * mesh_infos.vertices[i] +
                                       transform.rotation * (mesh_infos.normals[i].normalized * normalOffset),
                                       Quaternion.identity, scale * globalScale));

            // Set color array in the property block
            // RGBA values are set to the color array which will be used in the instanced shader
            c[0] = mesh_infos.colors[i].r;
            c[1] = mesh_infos.colors[i].g;
            c[2] = mesh_infos.colors[i].b;
            colors[i - startIndex] = c;
            // colors[i - startIndex] = new Vector4(mesh_infos.colors[i].r, mesh_infos.colors[i].g,
            //                                      mesh_infos.colors[i].b, 1);

        }

        propertyBlock.SetVectorArray("_Color", colors);
        RenderParams param = new RenderParams(pointMaterial);
        param.matProps = propertyBlock;
        // param.layer = this.gameObject.layer;
        // param.camera = null;
        // Draw the current mesh with the property block (colors are set in the shader)

        Graphics.RenderMeshInstanced(param, pointMesh, 0, matrices);

        startIndex = endIndex;
        endIndex = Mathf.Min(startIndex + maxInstancesPerDrawCall, mesh_infos.vertexCount);
        pointCount += maxInstancesPerDrawCall;
    }
}

    public void Generate(MeshInfos meshInfos, MeshTopology topology)
    {

        int vertexCount = meshInfos.vertexCount;
        int meshCount = (int)Mathf.Ceil(vertexCount / (float)verticesMax);
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



