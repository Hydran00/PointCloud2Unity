using System;
using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using PointCloudExporter;


namespace PC2
{
    public class PointCloudSubscriber : MonoBehaviour
    {
        ROSConnection ros;
        private byte[] byteArray;
        public bool isMessageReceived = false;

        private int size;
    
        private Vector3[] pcl;
        private Color[] pcl_color;

        private Vector3[] pcl_normals;

        private Vector3[] pcl_bounds;

        MeshInfos mesh;
        int width;
        int height;
        int row_step;
        int point_step;
        int i;

        public string topicName = "/camera/camera/depth/color/points";
        protected void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<PointCloud2Msg>(topicName, ReceiveMessage);
            mesh = new MeshInfos();
        }

        public void Update()
        {
            if (isMessageReceived)
            {
                PointCloudRendering();
                isMessageReceived = false;
            }

        }

        protected void ReceiveMessage(PointCloud2Msg message)
        
        {
            i++;
            size = message.data.GetLength(0);
            byteArray = new byte[size];
            byteArray = message.data;
            width = (int)message.width;
            height = (int)message.height;
            row_step = (int)message.row_step;
            point_step = (int)message.point_step;

            size = size / point_step;
            isMessageReceived = true;
        }

        void PointCloudRendering()
        {
            pcl = new Vector3[size];
            pcl_color = new Color[size];
            pcl_normals = new Vector3[size]; 
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

            for (int n = 0; n < size; n++)
            {
                x_posi = n * point_step + 0;
                y_posi = n * point_step + 4;
                z_posi = n * point_step + 8;

                x = BitConverter.ToSingle(byteArray, x_posi);
                y = BitConverter.ToSingle(byteArray, y_posi);
                z = BitConverter.ToSingle(byteArray, z_posi);


                rgb_posi = n * point_step + 16;

                b = byteArray[rgb_posi + 0];
                g = byteArray[rgb_posi + 1];
                r = byteArray[rgb_posi + 2];

                r = r / rgb_max;
                g = g / rgb_max;
                b = b / rgb_max;

                pcl[n] = new Vector3(x, z, y);
                pcl_color[n] = new Color(r, g, b);
                pcl_normals[n] = new Vector3(1f, 1f, 1f);
            }
            mesh.vertices = pcl;
            mesh.colors = pcl_color;
            mesh.normals = pcl_normals;
            mesh.vertexCount = size; 
            mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
        }

        public Vector3[] GetPCL()
        {
            return pcl;
        }

        public Color[] GetPCLColor()
        {
            return pcl_color;
        }
        public MeshInfos GetMeshInfos()
        {
            return mesh;
        }
    }
}
