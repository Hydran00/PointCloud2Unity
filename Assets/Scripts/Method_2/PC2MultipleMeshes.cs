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
using MeshBuffer;
public class PC2MultipleMeshes : MonoBehaviour
{
    public int verticesMax = 65535;
    public Camera CurrentCamera;
    public bool UseNormals = true;
    public const bool GPU_instancing = true;
    public Mesh pointMesh;
    // public Camera CurrentCamera;
    public bool faceCamera = true;
    public float normalOffset = 0f;
    [Range(0f, 0.08f)]
    public float globalScale = 0.1f;
    public Vector3 scale = Vector3.one;

    public Material pointMaterialGPU;

    ROSConnection ros;
    public string topicName = "/camera/camera/depth/color/points";

    int i = 0;
    [Header("MAKE SURE THESE LISTS ARE MINIMISED OR EDITOR WILL CRASH")]

    bool isMessageReceived = false;

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

    private const int maxInstancesPerDrawCall = 1023;

    List<Matrix4x4[]> matrices;
    List<Vector4[]> colors;

    List<MaterialPropertyBlock> propertyBlocks;
    List<RenderParams> renderParams;
    private int chunks_num = 0;


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
    }
    void Update()
    {
        // update every fps interval
        if (isMessageReceived)
        {
            isMessageReceived = false;
            ParsePointCloud();
            UpdateMesh();
        }
        DrawMesh();

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
        size = Mathf.Min(size, verticesMax);
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
        if (GPU_instancing)
        {
            GenerateGPUInstanced();
        }
    }
    void DrawMesh()
    {

        if (GPU_instancing)
        {
            for (int i = 0; i < chunks_num; i++)
            {
                Graphics.RenderMeshInstanced(renderParams[i], pointMesh, 0, matrices[i]);
            }
        }
    }

    Matrix4x4 GetPointTransforms(int i)
    {
        Vector3 position = transform.position + transform.rotation * mesh_infos.vertices[i] +
                       transform.rotation * (mesh_infos.normals[i].normalized * normalOffset);

        Quaternion rotation = Quaternion.identity;
        if (faceCamera)
        {
            rotation = Quaternion.LookRotation(CurrentCamera.transform.position - position);
        }
        else
        {
            rotation = transform.rotation * Quaternion.LookRotation(mesh_infos.normals[i]);
        }
        return Matrix4x4.TRS(position, rotation, scale * globalScale);
    }

    void GenerateGPUInstanced()
    {


        int startIndex = 0;
        int endIndex = Mathf.Min(maxInstancesPerDrawCall, mesh_infos.vertexCount);
        int pointCount = 0;
        chunks_num = (int)Mathf.Ceil(mesh_infos.vertexCount / (float)maxInstancesPerDrawCall);


        matrices = new List<Matrix4x4[]>();
        colors = new List<Vector4[]>();
        Vector4 c = new Vector4(1, 1, 1, 1);
        propertyBlocks = new List<MaterialPropertyBlock>(chunks_num);
        renderParams = new List<RenderParams>(chunks_num);

        // initialize the matrices and colors lists
        for (int i = 0; i < chunks_num; i++)
        {
            matrices.Add(new Matrix4x4[maxInstancesPerDrawCall]);
            colors.Add(new Vector4[maxInstancesPerDrawCall]);
            propertyBlocks.Add(new MaterialPropertyBlock());
            renderParams.Add(new RenderParams(pointMaterialGPU));
        }

        // while (pointCount < mesh_infos.vertexCount)
        for (int chunkIndex = 0; chunkIndex < chunks_num; chunkIndex++)
        {
            // Generate a single chunk of points
            for (int i = startIndex; i < endIndex; i++)
            {
                // matrices.Add(GetPointTransforms(i));
                matrices[chunkIndex][i - startIndex] = GetPointTransforms(i);
                // Set color array in the property block
                // RGBA values are set to the color array which will be used in the instanced shader
                c[0] = mesh_infos.colors[i].r;
                c[1] = mesh_infos.colors[i].g;
                c[2] = mesh_infos.colors[i].b;
                colors[chunkIndex][i - startIndex] = c;

            }
            var pb = new MaterialPropertyBlock();
            pb.SetVectorArray("_Color", colors[chunkIndex]);
            propertyBlocks[chunkIndex] = pb;

            var param = renderParams[chunkIndex];
            param.matProps = propertyBlocks[chunkIndex];
            renderParams[chunkIndex] = param;


            startIndex = endIndex;
            endIndex = Mathf.Min(startIndex + maxInstancesPerDrawCall, mesh_infos.vertexCount);
            pointCount += maxInstancesPerDrawCall;
        }
    }

}



