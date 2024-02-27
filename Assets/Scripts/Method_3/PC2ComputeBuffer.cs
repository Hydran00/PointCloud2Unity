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
public class PC2ComputeBuffer : MonoBehaviour
{

    public Material material;

    public Camera CurrentCamera;
    // public bool UseNormals = true;
    public Mesh pointMesh;
    public bool faceCamera = true;
    public float normalOffset = 0f;
    [Range(0f, 0.08f)]
    public Vector3 PointMeshScale = Vector3.one;
    public float globalScale = 0.1f;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private Bounds bounds;

    public PointCloud2Decoder PC2Decoder;

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
    void Start() { }

    void Update()
    {
        if (PC2Decoder.isMessageReceived)
        {
            PC2Decoder.ParsePointCloud();
            FillBuffers();
            PC2Decoder.isMessageReceived = false;
        }
        if (PC2Decoder.mesh_infos.vertexCount > 0)
        {
            Debug.Log("Drawing");
            Graphics.DrawMeshInstancedIndirect(pointMesh, 0, material, bounds, argsBuffer);
        }
    }
    private void FillBuffers()
    {
        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == PC2Decoder.mesh_infos.vertexCount, others are only relevant if drawing submeshes.
        args[0] = (uint)pointMesh.GetIndexCount(0);
        args[1] = (uint)PC2Decoder.mesh_infos.vertexCount;
        args[2] = (uint)pointMesh.GetIndexStart(0);
        args[3] = (uint)pointMesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given PC2Decoder.mesh_infos.vertexCount.
        MeshProperties[] properties = new MeshProperties[PC2Decoder.mesh_infos.vertexCount];
        for (int i = 0; i < PC2Decoder.mesh_infos.vertexCount; i++)
        {
            MeshProperties props = new MeshProperties();
            props.mat = GetPointTransforms(i);
            
            props.color = new Color(PC2Decoder.mesh_infos.colors[i].r, PC2Decoder.mesh_infos.colors[i].g, PC2Decoder.mesh_infos.colors[i].b, 1);

        properties[i] = props;
    }

    meshPropertiesBuffer = new ComputeBuffer(PC2Decoder.mesh_infos.vertexCount, MeshProperties.Size());
    meshPropertiesBuffer.SetData(properties);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }


Matrix4x4 GetPointTransforms(int i)
{
    Vector3 position = transform.position + transform.rotation * PC2Decoder.mesh_infos.vertices[i] +
                   transform.rotation * (PC2Decoder.mesh_infos.normals[i].normalized * normalOffset);

    Quaternion rotation = Quaternion.identity;
    if (faceCamera)
    {
        rotation = Quaternion.LookRotation(CurrentCamera.transform.position - position);
    }
    else
    {
        rotation = transform.rotation * Quaternion.LookRotation(PC2Decoder.mesh_infos.normals[i]);
    }
    return Matrix4x4.TRS(position, rotation, PointMeshScale * globalScale);
}
}




