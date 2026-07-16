using UnityEngine;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-300)] // 最早更新
public class HPGridManager : MonoBehaviour
{
    public static HPGridManager Instance { get; private set; }

    [Header("渲染中心设置")]
    public HPFloatingPoint targetObject;
    
    [Tooltip("将目标对象拉回原点的阈值")]
    public float moveThreshold = 1000f;

    [Header("状态 (只读)")]
    public HPPos globalOrigin; 

    public static HPPos GlobalOrigin => Instance ? Instance.globalOrigin : new HPPos(0, 0, 0, 0, 0, 0);

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {            
            if (Application.isPlaying) Destroy(gameObject);
            else Instance = this;
        }

        if (!Application.isPlaying && globalOrigin.Equals(default(HPPos)))
        {
            globalOrigin = new HPPos(0, 0, 0, 0, 0, 0);
        }
    }

    private void LateUpdate()
    {
        if (targetObject == null) return;

        HPPos focusPos = targetObject.position;
        HPPos deltaOrigin = focusPos - globalOrigin;

        // 只要飞出阈值，就以玩家当前位置为新的原点
        if (deltaOrigin.sqrMagnitude > moveThreshold * moveThreshold)
        {
            globalOrigin = focusPos;
        }
    }
}