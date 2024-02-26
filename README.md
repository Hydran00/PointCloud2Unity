# PointCloud2Unity
A Unity package containing different possible implementations for visualizing the PointCloud2 data coming from ROS2 through the TCP-Connector.


### Proposed methods:
1. Creating a single mesh with all the points. Then points will render a triangle or a sphere using the geometry shader.
2. Exploit GPU Instancing and render directly each points as a single mesh (quad, cube, sphere).
3. Variant of method 2 in which ComputeBuffers are used to store the data and then render the points.

Each methods uses a particular shader which is located in the same folder of the script.


## How to use it:
### ROS Users:
1. Install the ROS-TCP-Connector package from [here](https://github.com/Unity-Technologies/ROS-TCP-Connector) and ROS-TCP-Endopoint ros-package from [here](https://github.com/Unity-Technologies/ROS-TCP-Endpoint). You can follow [this](https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/quick_setup.md) guide.
2. Set up the ROS-TCP-Connector and ROS-TCP-Endpoint to communicate with your ROS2 environment.
3. Clone this repository and open the project in Unity.
4. open one of the `Method` scene.  
5. Set your topic name and other options in the script inspector of the `PCL` object:  
    - Activate <b>Use Normals</b> if your PointCloud2 message contains normals.  
    You can check if it does by running:  
        ```ros2 topic echo /your_topic_name```   
        in your terminal and look for the field `normals_`.  
     This will modify offsets of data in the pointcloud decoder.  
    - Enable <b>GPU instancing</b> for high performance (suggested).  
    - Set your favourite  <b>mesh for each point</b> in the `Point Mesh' field.  
    - Set the <b>scale</b> of each mesh point.
    - Define <b>offsets</b> for position and rotation if you need to.
    - Set the <b>topic name</b> of the PointCloud2 message you want to visualize.
6. Run the scene and you should see the pointcloud being visualized in Unity.
### Non-ROS Users:
In this case you have to write your own code to fill the MeshInfo struct with the data you want to visualize. You can then use the functions 
implemented in this package.
## Common Parameters
The three methods share some common parameters:
- Vertices Max: The maximum number of vertices that will be extracted from the pointcloud for visualization.
- Topic Name: The name of the topic you want to visualize.
- Use Normals: If the pointcloud contains normals, this will modify the offsets of the data in the pointcloud decoder algorithm.
Normals are also used in Method 1 to rotate triangles towards the camera.


## Method 1
This method creates a single mesh with all the points. Then points will render a triangle or a sphere using the geometry shader.
This method seems to be the most performant.





## Preview
You can modify the size of the points and the mesh used to represent them.
<table>
    <tr>
        <td> <b>Quads</b> </td>
        <td> <b>Cubes</b> </td>
        <td> <b>Spheres</b> </td>
    </tr>
        <td> <img src="imgs/quad.png" alt="Drawing quads" style="width: 250px;"/> </td>
        <td> <img src="imgs/cubes.png" alt="Drawing cubes" style="width: 250px;"/> </td>
        <td> <img src="imgs/spheres.png" alt="Drawing spheres" style="width: 250px;"/> </td>
    <tr>
    <td> <b>Changing size</b> </td>
    </tr>
        <td> <img src="imgs/size1.png" alt="Changing size" style="width: 250px;"/> </td>
        <td> <img src="imgs/size2.png" alt="Changing size" style="width: 250px;"/> </td>
        <td> <img src="imgs/size3.png" alt="Changing size" style="width: 250px;"/> </td>
    <tr>
</table>


## Tips for improving performances
- Use GPU instancing for rendering the pointcloud. This will allow you to render thousands of points with a single draw call.
- Preprocess the pointcloud filtering some points. An simple example with segmentation + voxel grid is provided [here](https://github.com/Hydran00/PC2-Filter-ROS2).



## References
For this project I took inspiration from the following repositories/websites:  
- [Pcx](https://github.com/keijiro/Pcx)
- [GPU Instancing tutorial](https://toqoz.fyi/thousands-of-meshes.html)
- [PointCloud Processing tutorial](https://sketchfab.com/blogs/community/tutorial-processing-point-cloud-data-unity/)
- [PointCloud Streaming](https://github.com/inmo-jang/unity_assets/tree/master/PointCloudStreaming)
- [Vertex Point Cloud](https://github.com/keenanwoodall/VertexPointCloud/tree/master)
