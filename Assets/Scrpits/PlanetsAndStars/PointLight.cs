using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class PointLight : MonoBehaviour
{
    public Transform mainCamera;
    public Transform star;

    [Header("Settings")]
    public float baseScale = 1.0f;
    public float minScale = 0.1f;
    
    [Tooltip("基准参考距离")]
    public float referenceDistance = 150.0f;

    LensFlareComponentSRP flareComponent;

    void Awake()
    {
        flareComponent = GetComponent<LensFlareComponentSRP>();
    }
    void Update()
    {

        if (mainCamera == null || star == null) return;

        float3 diff = mainCamera.position - star.position;
        double dist = math.length(diff);

        float3 dir = math.normalize(diff);
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 3. 计算物理衰减 (Near Large/Bright, Far Small/Dim)
        if (flareComponent != null)
        {
            // 归一化距离因子
            float x = (float)(dist / referenceDistance);
            
            // 衰减公式：1 / (1 + x^n) 
            // 使用 +1.0 保证距离为0时系数为1，避免数值爆炸
            float attenuation = 1.0f / (1.0f + Mathf.Pow(x, 2));

            flareComponent.scale = Mathf.Max(baseScale * attenuation, minScale);
        }
    }
}
