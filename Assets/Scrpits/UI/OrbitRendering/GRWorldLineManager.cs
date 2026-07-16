using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using AstralLineEngine;

[DefaultExecutionOrder(-100)] // 在每帧原点更新完再绘制
public class GRWorldLineManager : MonoBehaviour
{
    public static GRWorldLineManager Instance;

    private NativeList<WorldLinePoint> centralWorldLine;

    [Header("参考系与缩放")]
    public GRPhysicsObject referenceCenter; // 参考系中心
    public float viewScale = 1.0f;
    public Color centralStartColor = Color.red;
    public Color centralEndColor = Color.red;
    public int maxHistory = 5000;
    public float lineWidth = 1f;
    public double areaThreshold = 1;

    [Header("相机设置")]
    public Camera LineCamera;

    void Awake()
    {
        Instance = this;
        centralWorldLine = new NativeList<WorldLinePoint>(maxHistory, Allocator.Persistent);
    }

    protected virtual void OnEnable()
    {
        Application.onBeforeRender += DrawLinesBeforeRender;
    }

    protected virtual void OnDisable()
    {
        Application.onBeforeRender -= DrawLinesBeforeRender;
    }

    // 修复内存泄漏：NativeList 必须销毁
    private void OnDestroy()
    {
        if (centralWorldLine.IsCreated)
        {
            centralWorldLine.Dispose();
        }
    }

    private void DrawLinesBeforeRender()
    {
        HPPos currentOrigin = HPGridManager.Instance != null ? HPGridManager.GlobalOrigin : new HPPos(0, 0, 0, 0, 0, 0);
        OnRecalculateLinePosition(currentOrigin);
    }

    public void RecordCentralPoint(double t)
    {        
        centralWorldLine.Add(new WorldLinePoint(t, double2.zero));
        if (centralWorldLine.Length > maxHistory) centralWorldLine.RemoveAt(0);
    }

    protected virtual void OnRecalculateLinePosition(HPPos globalOrigin)
    {
        var managedObjects = GRPhysicsEngine.Instance.managedObjects;
        if (managedObjects.Count == 0) return;

        // 1. 获取 Builder
        var builder = ALEManager.GetBuilder(true);
        builder.cameraTargets = new Camera[] { LineCamera };

        float4x4 vpMatrix = LineCamera.projectionMatrix * LineCamera.worldToCameraMatrix;
        float2 res = new float2(Screen.width, Screen.height);
        double2 currentHPOrigin = ((double3)globalOrigin).xz;
        double2 finalJobOrigin = (referenceCenter != null) ? currentHPOrigin - referenceCenter.x : currentHPOrigin;

        var stationaryFrame = GRPhysicsEngine.Instance.StationaryFrame;
        bool hasReference = referenceCenter != null;

        // --- 关键：初始化 Handle ---
        JobHandle combinedHandle = default;

        // 1. 中心天体项
        if (centralWorldLine.Length >= 2)
        {
            var centralJob = new WorldLineDrawJob {
                TargetLine = centralWorldLine.AsArray(),
                ReferenceLine = hasReference ? referenceCenter.worldLine.AsArray() : stationaryFrame,
                builder = builder,
                universeOrigin = finalJobOrigin,
                viewScale = viewScale,
                startColor = centralStartColor,
                endColor = centralEndColor,
                width = lineWidth,
                vpMatrix = vpMatrix,
                screenRes = res,
                areaThreshold = areaThreshold / 100,
                enableRender = true,
                isRelative = hasReference
            };
            // 第一次调度
            combinedHandle = centralJob.Schedule();
        }

        // 2. 历史轨迹
        foreach (var obj in managedObjects)
        {
            if (obj.worldLine.Length < 2) continue;

            var job = new WorldLineDrawJob {
                TargetLine = obj.worldLine.AsArray(),
                ReferenceLine = (hasReference && obj != referenceCenter) ? referenceCenter.worldLine.AsArray() : stationaryFrame,
                builder = builder,
                universeOrigin = finalJobOrigin,
                viewScale = viewScale,
                startColor = obj.startHistoricalColor,
                endColor = obj.endHistoricalColor,
                width = lineWidth,
                vpMatrix = vpMatrix,
                screenRes = res,
                areaThreshold = areaThreshold / 100,
                enableRender = true,
                isRelative = hasReference && (obj != referenceCenter)
            };
            // --- 核心修复：传入上一个 combinedHandle 作为依赖 ---
            // 这样所有 Job 会排队写入同一个 builder.buffer，解决 Write-Write 冲突
            combinedHandle = job.Schedule(combinedHandle);
        }

        // 3. 预测轨迹 (逻辑同上)
        if (GROrbitPrediction.Instance != null && GROrbitPrediction.Instance.enablePrediction)
        {
            foreach (var obj in managedObjects)
            {
                if (obj.predictedWorldLine.Length < 2) continue;

                var job = new WorldLineDrawJob {
                    TargetLine = obj.predictedWorldLine.AsArray(),
                    ReferenceLine = (hasReference && obj != referenceCenter) ? referenceCenter.predictedWorldLine.AsArray() : stationaryFrame,
                    builder = builder,
                    universeOrigin = finalJobOrigin,
                    viewScale = viewScale,
                    startColor = obj.startPredictColor,
                    endColor = obj.endPredictColor,
                    width = lineWidth,
                    vpMatrix = vpMatrix,
                    screenRes = res,
                    areaThreshold = areaThreshold / 100,
                    enableRender = true,
                    isRelative = hasReference && (obj != referenceCenter)
                };
                combinedHandle = job.Schedule(combinedHandle);
            }
        }

        // 4. 强制完成并释放
        combinedHandle.Complete();
        builder.Dispose(); // 同步释放，确保下一物理帧开始前 buffer 已销毁
    }
}