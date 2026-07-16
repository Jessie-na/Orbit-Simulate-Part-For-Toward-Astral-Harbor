using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;

[DefaultExecutionOrder(-450)]
public class GRPhysicsEngine : MonoBehaviour
{ 
    [Header("能量漂移指示（相对漂移）")]
    public double initialEnergy = 0;
    public double energyDrift = 0;
    
    
    public static GRPhysicsEngine Instance;

    [Header("全局坐标时")]
    public double globalTime = 0; // 全局坐标时 t

    [Header("物理常数")]
    public double G = 1; public double c = 1; public double M = 1; public double a = 0;

    [Header("时间控制")]
    [Tooltip("时间加速倍率")]
    public double timeWarp = 1.0;
    [Tooltip("物理演算步长（不建议设大于50）")]
    public double targetPhysicsStep = 0.02;
    [Tooltip("是否启用物理加速")]
    public bool isPhysicsWarpActive = false;

    public List<GRPhysicsObject> managedObjects = new List<GRPhysicsObject>();
    public NativeArray<ObjectStateData> statesInput;
    public NativeArray<WorldLinePoint> StationaryFrame;


    double accumulator;
    bool isFirstFrame = true;

#region 初始化相关
    void Awake()
    {
        Instance = this;
        // 初始化占位数组
        StationaryFrame = new NativeArray<WorldLinePoint>(1, Allocator.Persistent);
        // 这是中心天体的世界线
        StationaryFrame[0] = new WorldLinePoint(0, double2.zero);
    }

    // 将物理对象进行登记
    public void Register(GRPhysicsObject obj) => managedObjects.Add(obj);
    public void Unregister(GRPhysicsObject obj) => managedObjects.Remove(obj);

    // 初始化运行一次物理引擎进行热机，不然能量和牛顿项引力计算会出问题
    // 这里除了演算 Job 时的步长设为0，其他都和 FixedUpdate 里基本一致
    // P2：引入 Jolt 后这个方法还承担了初始化刚体数据的工作
    private void InitializePhysics()
    {
        int count = managedObjects.Count;
        UpdateNativeArrays(count);

        for (int i = 0; i < count; i++)
        {
            statesInput[i] = new ObjectStateData
            {
                x = managedObjects[i].x,
                u = managedObjects[i].u,
                mass = managedObjects[i].mass,
                deltaProper = 0
            };
        }

        var initJob = new GRPhysicsJob
        {
            States = statesInput,
            aM = new double2(a, M),
            G = G,
            c = c,
            dt = 0,
            subStepCount = 1
        };
        initJob.Schedule().Complete();

        // 更新能量
        double globalH = 0;
        for (int i = 0; i < count; i++)
        {
            globalH += statesInput[i].energy * statesInput[i].mass;
            managedObjects[i].H = statesInput[i].energy;
            managedObjects[i].dxdt = statesInput[i].dxdt;
            managedObjects[i].metric = statesInput[i].metric;
        }
        initialEnergy = globalH;
        isFirstFrame = false;
    }
    #endregion

    // 生命周期
    void FixedUpdate()
    {
        int count = managedObjects.Count;
        if (count == 0) return;
        if (isFirstFrame) InitializePhysics();

        // 维护两个 NativeArray
        UpdateNativeArrays(count);

        RunOrbitWarp(count);

        // 能量计算统计 与 世界线采样记录
        RecordAndStatusUpdate(count);
    }
    
    /// <summary>
    /// 轨道加速模式，Job直接处理单帧多步长的模拟
    /// </summary>
    /// <param name="count"></param>
    private void RunOrbitWarp(int count)
    {
        // 1. 同步数据到 Native
        for (int i = 0; i < count; i++)
        {
            SyncManagedToNative(i);
        }

        // 2. 计算本帧总步数
        accumulator += Time.fixedDeltaTime * timeWarp;
        int subSteps = (int)math.floor(accumulator / targetPhysicsStep);
        float finalDt = 0;

        if (subSteps > 0)
        {
            // 安全限制，防止单帧步数过多导致驱动超时（TDR）
            subSteps = math.min(subSteps, 1000);

            // 3. 调度一次 Job 算完所有子步
            var job = new GRPhysicsJob
            {
                States = statesInput,
                aM = new double2(a, M),
                G = G,
                c = c,
                dt = targetPhysicsStep,
                subStepCount = subSteps
            };
            job.Schedule().Complete();


            // 4. 更新时间进度
            globalTime += subSteps * targetPhysicsStep;
            finalDt += (float)(subSteps * targetPhysicsStep);
            accumulator -= subSteps * targetPhysicsStep;
        }
        
        // 5. 回传数据
        for (int i = 0; i < count; i++)
        {
            SyncNativeToManaged(i, finalDt);
        }
    }


#region 辅助同步方法
    /// <summary>
    /// 将 managedObjects 的数据传至 statesInput
    /// </summary>
    /// <param name="i"></param>
    private void SyncManagedToNative(int i)
    {
        statesInput[i] = new ObjectStateData
        {
            x = managedObjects[i].x,
            u = managedObjects[i].u,
            mass = managedObjects[i].mass,
            dxdt = managedObjects[i].dxdt,
            energy = managedObjects[i].H,
            deltaProper = 0
        };
    }

    /// <summary>
    /// 将 statesInput 的数据传至 managedObjects
    /// </summary>
    /// <param name="i"></param>
    private void SyncNativeToManaged(int i, float dt)
    {
        var data = statesInput[i];
        managedObjects[i].metric = data.metric;

        managedObjects[i].x = data.x;
        managedObjects[i].u = data.u + managedObjects[i].thrust * dt;
        managedObjects[i].UpdateRot(dt);
        managedObjects[i].H = data.energy;
        managedObjects[i].dxdt = data.dxdt;
        managedObjects[i].properTime += data.deltaProper;
    }
    /// <summary>
    /// 遍历每一个物理对象及中心天体进行能量计算与历史线的存储
    /// </summary>
    /// <param name="count"></param>
    private void RecordAndStatusUpdate(int count)
    {
        double globalH = 0;
        GetComponent<GRWorldLineManager>().RecordCentralPoint(globalTime);
        for (int i = 0; i < count; i++)
        {
            managedObjects[i].RecordWorldLine(globalTime);
            globalH += managedObjects[i].H * managedObjects[i].mass;
        }
        energyDrift = (initialEnergy - globalH) / initialEnergy;
    }
    /// <summary>
    /// 更新 statesInput
    /// </summary>
    /// <param name="count"></param>
    void UpdateNativeArrays(int count)
    {
        if (!statesInput.IsCreated || statesInput.Length != count)
        {
            if (statesInput.IsCreated) statesInput.Dispose();
            statesInput = new NativeArray<ObjectStateData>(count, Allocator.Persistent);
        }
    }
#endregion

    // 清除缓存
    void OnDestroy()
    {
        if (StationaryFrame.IsCreated) StationaryFrame.Dispose();
        if (statesInput.IsCreated) statesInput.Dispose();
    }
}