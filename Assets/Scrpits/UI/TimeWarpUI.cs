using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using Unity.Mathematics;

public class TimeWarpUI : MonoBehaviour
{
    [Header("加速倍率预设")]
    public List<TimeWarpPreset> presets = new List<TimeWarpPreset>()
    {
        new (1, 0.02f),
        new (5, 0.02f),
        new (10, 0.04f),
        new (100, 1),
        new (1000, 1),
        new (10000, 1),
        new (100000, 10),
        new (1000000, 20)
    };
    [Header("当前倍率")]
    public int currentLevel = 0;
    [Header("插值设置")]
    [Tooltip("这个以后可以集成到settings里，但是我还没写，那就算了")]
    public float duration = 0.5f;
    GRPhysicsEngine GRPE;
    TextMeshProUGUI timeWarpUI;
    int count;

    private Coroutine transitionRoutine;


    void Start()
    {
        count = presets.Count;
        GRPE = GRPhysicsEngine.Instance;
        timeWarpUI = GetComponent<TextMeshProUGUI>();
    }
    
    void LateUpdate()
    {
        if (!timeWarpUI) return;
        timeWarpUI.text = $"TimeWarp = {GRPE.timeWarp:F1}";
    }

    #region 从PlayerInput调用的Actions
    // 改变时间加速倍率
    public void ChangeTimeWarp(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int nextLevel = math.clamp(currentLevel + (int)context.ReadValue<float>(), 0, presets.Count - 1);
        if (nextLevel == currentLevel) return;

        currentLevel = nextLevel;

        // 停止之前的过渡，开始新的过渡
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(DoTransition(presets[currentLevel]));
    }
    #endregion
    private System.Collections.IEnumerator DoTransition(TimeWarpPreset target)
    {
        double sWarp = GRPE.timeWarp;
        double sDt = GRPE.targetPhysicsStep;

        // 简洁的进度循环
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            GRPE.timeWarp = math.lerp(sWarp, target.timeWarp, (double)t);
            GRPE.targetPhysicsStep = math.lerp(sDt, target.dt, (double)t);
            timeWarpUI.text = $"TimeWarp: {(int)GRPE.timeWarp}x";
            yield return null;
        }

        // 最后硬锁定确保数值准确
        GRPE.timeWarp = target.timeWarp;
        GRPE.targetPhysicsStep = target.dt;
        timeWarpUI.text = $"TimeWarp: {(int)GRPE.timeWarp}x";
    }
}

[Serializable]
public struct TimeWarpPreset
{
    [Tooltip("时间加速倍率")]
    public float timeWarp;
    
    [Tooltip("该倍率下的积分步长")]
    public float dt;

    public TimeWarpPreset(float warp, float step)
    {
        timeWarp = warp;
        dt = step;
    }
}
