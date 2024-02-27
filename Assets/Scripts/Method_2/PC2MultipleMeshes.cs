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
public class PC2MultipleMeshes : MonoBehaviour
{
    public Camera CurrentCamera;

    public Mesh pointMesh;
    public bool faceCamera = true;
    public float normalOffset = 0f;
    [Range(0f, 0.08f)]
    public float globalScale = 0.1f;
    public Vector3 scale = Vector3.one;
    public Material pointMaterialGPU;
    public PointCloud2Decoder PC2Decoder;

    private const int maxInstancesPerDrawCall = 1023;

    List<Matrix4x4[]> matrices;
    List<Vector4[]> colors;

    List<MaterialPropertyBlock> propertyBlocks;
    List<RenderParams> renderParams;
    private int chunks_num = 0;
    void Start() { }
    void Update()
    {
        if (PC2Decoder.isMessageReceived)
        {
            PC2Decoder.ParsePointCloud();
            GenerateGPUInstanced();
            PC2Decoder.isMessageReceived = false;
        }
        DrawMesh();
    }

    void DrawMesh()
    {
        for (int i = 0; i < chunks_num; i++)
        {
            Graphics.RenderMeshInstanced(renderParams[i], pointMesh, 0, matrices[i]);
        }
    }

    void GenerateGPUInstanced()
    {
        int startIndex = 0;
        int endIndex = Mathf.Min(maxInstancesPerDrawCall, PC2Decoder.mesh_infos.vertexCount);
        int pointCount = 0;
        chunks_num = (int)Mathf.Ceil(PC2Decoder.mesh_infos.vertexCount / (float)maxInstancesPerDrawCall);

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

        // while (pointCount < PC2Decoder.mesh_infos.vertexCount)
        for (int chunkIndex = 0; chunkIndex < chunks_num; chunkIndex++)
        {
            // Generate a single chunk of points
            for (int i = startIndex; i < endIndex; i++)
            {
                // matrices.Add(GetPointTransforms(i));
                matrices[chunkIndex][i - startIndex] = GetPointTransforms(i);
                // Set color array in the property block
                // RGBA values are set to the color array which will be used in the instanced shader
                c[0] = PC2Decoder.mesh_infos.colors[i].r;
                c[1] = PC2Decoder.mesh_infos.colors[i].g;
                c[2] = PC2Decoder.mesh_infos.colors[i].b;
                colors[chunkIndex][i - startIndex] = c;

            }
            var pb = new MaterialPropertyBlock();
            pb.SetVectorArray("_Color", colors[chunkIndex]);
            propertyBlocks[chunkIndex] = pb;

            var param = renderParams[chunkIndex];
            param.matProps = propertyBlocks[chunkIndex];
            renderParams[chunkIndex] = param;


            startIndex = endIndex;
            endIndex = Mathf.Min(startIndex + maxInstancesPerDrawCall, PC2Decoder.mesh_infos.vertexCount);
            pointCount += maxInstancesPerDrawCall;
        }
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
        return Matrix4x4.TRS(position, rotation, scale * globalScale);
    }

}



