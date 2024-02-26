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
public class PC2ComputeBuffer : MonoBehaviour
{

    private const int verticesMax = 65535;
    public Material material;

    public Camera CurrentCamera;
    public bool UseNormals = true;
    public Mesh pointMesh;
    public bool faceCamera = true;
    public float normalOffset = 0f;
    [Range(0f, 0.08f)]
    public Vector3 PointMeshScale = Vector3.one;
    public float globalScale = 0.1f;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private Bounds bounds;

    private ROSConnection ros;
    public string topicName = "/camera/camera/depth/color/points";

    int i = 0;
    [Header("MAKE SURE THESE LISTS ARE MINIMISED OR EDITOR WILL CRASH")]

    bool isMessageReceived = false;

    private MeshInfos mesh_infos;
    private byte[] byteArray;

    private int RGB_OFFSET = 32;
    private const int NORMALS_OFFSET = 16;
    private int size;
    private int width;
    private int height;
    private int row_step;
    private int point_step;
    // private Vector4[] colors;

    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }
    private void FillBuffers()
    {
        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == mesh_infos.vertexCount, others are only relevant if drawing submeshes.
        args[0] = (uint)pointMesh.GetIndexCount(0);
        args[1] = (uint)mesh_infos.vertexCount;
        args[2] = (uint)pointMesh.GetIndexStart(0);
        args[3] = (uint)pointMesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given mesh_infos.vertexCount.
        MeshProperties[] properties = new MeshProperties[mesh_infos.vertexCount];
        for (int i = 0; i < mesh_infos.vertexCount; i++)
        {
            MeshProperties props = new MeshProperties();
            
            props.mat = GetPointTransforms(i);
            props.color = new Color(mesh_infos.colors[i].r, mesh_infos.colors[i].g, mesh_infos.colors[i].b, 1); 

            properties[i] = props;
        }

        meshPropertiesBuffer = new ComputeBuffer(mesh_infos.vertexCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointCloud2Msg>(topicName, StorePointCloud);
        mesh_infos = new MeshInfos();
        if (UseNormals == false)
        {
            RGB_OFFSET = 16;
        }
        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * (5 + 1));
    }
    void Update()
    {
        if (isMessageReceived)
        {
            ParsePointCloud();
            FillBuffers();
            isMessageReceived = false;
        }
        if (mesh_infos.vertexCount > 0)
        {
            Graphics.DrawMeshInstancedIndirect(pointMesh, 0, material, bounds, argsBuffer);
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
        return Matrix4x4.TRS(position, rotation, PointMeshScale * globalScale);
    }
}




