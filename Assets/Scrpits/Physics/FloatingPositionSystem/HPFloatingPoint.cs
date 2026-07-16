using UnityEngine;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-200)] // 检查完原点后再进行位置更新
public class HPFloatingPoint : MonoBehaviour
{
    [Tooltip("真实的宇宙绝对坐标 (由双精度物理或轨道引擎读写)")]
    public HPPos position; 

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            // ========================================================================
            // 【核心架构：绝对的单向数据流】
            // 物理数据 (HPPos) -> 减去渲染原点 -> 赋予 Unity Transform (Float)
            // 运行时绝对不允许 Transform 反向污染 position！
            // ========================================================================
            HPPos currentOrigin = HPGridManager.GlobalOrigin;
            
            // 绝对差值计算，转为 Float 给 Unity 渲染
            Vector3 relativePos = (Vector3)(position - currentOrigin);
            
            transform.position = relativePos;
            transform.hasChanged = false; // 重置标记，防止干扰其他逻辑
        }
        else
        {
            // ========================================================================
            // 编辑器辅助：允许在非运行状态下，通过拖拽物体来排布场景
            // ========================================================================
            if (transform.hasChanged)
            {
                HPPos currentOrigin = HPGridManager.Instance != null ? HPGridManager.GlobalOrigin : new HPPos(0, 0, 0, 0, 0, 0);
                position = currentOrigin + transform.position;
                position.Normalize();
                transform.hasChanged = false;
            }
        }
    }

#if UNITY_EDITOR
    // 监听 Inspector 里的数值修改
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        position.Normalize();
        
        HPPos currentOrigin = HPGridManager.Instance != null ? HPGridManager.GlobalOrigin : new HPPos(0, 0, 0, 0, 0, 0);
        transform.position = (Vector3)(position - currentOrigin);
        transform.hasChanged = false;
    }
#endif
}

