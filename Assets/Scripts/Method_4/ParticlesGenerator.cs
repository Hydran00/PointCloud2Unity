using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ParticlesGenerator : MonoBehaviour
{
    private Texture2D texColor;
    private Texture2D texPosScale;
    private VisualEffect vfx;
    uint resolution = 1024;

    // range
    [Range(0.001f, 0.5f)]
    public float particleSize = 0.01f;
    bool toUpdate = true;
    public PointCloud2Decoder PC2Decoder;
    uint particleCount;

    private void Start()
    {
        vfx = GetComponent<VisualEffect>();
    }

    private void Update()
    {
        if (PC2Decoder.isMessageReceived)
        {
            PC2Decoder.isMessageReceived = false;
            PC2Decoder.ParsePointCloud();
            SetParticles();
            DrawParticles();
        }
    }
    private void DrawParticles()
    {
        vfx.Reinit();
        vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
        vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
        vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPosScale);
        vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
    }
    public void SetParticles()
    {
        var positions = PC2Decoder.mesh_infos.vertices;
        var colors = PC2Decoder.mesh_infos.colors;
        particleCount = (uint)PC2Decoder.mesh_infos.vertexCount;

        texColor = new Texture2D(positions.Length > (int)resolution ? (int)resolution : positions.Length, Mathf.Clamp(positions.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
        texPosScale = new Texture2D(positions.Length > (int)resolution ? (int)resolution : positions.Length, Mathf.Clamp(positions.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
        int texWidth = texColor.width;
        int texHeight = texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                texColor.SetPixel(x, y, colors[index]);
                var data = new Color(positions[index].x, positions[index].y, positions[index].z, particleSize);
                texPosScale.SetPixel(x, y, data);
            }
        }
        texColor.Apply();
        texPosScale.Apply();
    }
}
