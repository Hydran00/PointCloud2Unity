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

public class MeshInfos
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public Color[] colors;
    public int vertexCount;
    public Bounds bounds;
}

// class to store the point cloud data
public class PointCloud2Decoder : MonoBehaviour
{

    const int NORMALS_OFFSET = 16;
    private int RGB_OFFSET = 32;
    public string topicName;
    public int verticesMax = 65535;
    public bool UseNormals = false;
    [System.NonSerialized]
    public int size = 0;
    private int width = 0;
    private int height = 0;
    private int row_step = 0;
    private int point_step = 0;

    public MeshInfos mesh_infos = new MeshInfos();
    private Bounds bounds;
    private byte[] byteArray;

    private ROSConnection ros;
    [System.NonSerialized]
    public bool isMessageReceived = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointCloud2Msg>(topicName, StorePointCloud);
        mesh_infos = new MeshInfos();
        mesh_infos.vertexCount = 0;
        mesh_infos.vertices = new Vector3[verticesMax];
        mesh_infos.colors = new Color[verticesMax];
        mesh_infos.normals = new Vector3[verticesMax];
        mesh_infos.vertexCount = verticesMax;
        mesh_infos.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
        byteArray = new byte[verticesMax];

        if (UseNormals == false)
        {
            RGB_OFFSET = 16;
        }
        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
    }

    private void StorePointCloud(PointCloud2Msg message)
    {
        // byteArray = new byte[size];
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
    public void ParsePointCloud()
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
}