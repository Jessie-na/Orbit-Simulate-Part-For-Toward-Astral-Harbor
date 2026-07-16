using UnityEngine;
using Unity.Mathematics;

// 让对象的模型大小向相机距离看齐
public class SimpleModel : MonoBehaviour
{
    public float scale = 0.5f;

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }
    void LateUpdate()
    {
        transform.localScale = new float3(1, 1, 1)
                             * mainCamera.transform.parent.localScale
                             * scale;
    }
}
