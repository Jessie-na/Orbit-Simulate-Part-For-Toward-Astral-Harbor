using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class GRPhysicsObject : MonoBehaviour
{
    public double3 acceleration;
    [Header("物理参数")]
    public double properTime = 0;
    public double2 x;
    public double2 u;
    //[HideInInspector] 
    public float rotationSpeed; // 绕y轴旋转，逆时针为正
    public double mass;
    [HideInInspector] public double2 dxdt;
    public Metric metric;
    private GRPhysicsEngine GRPE;


    public NativeList<WorldLinePoint> worldLine;
    public NativeList<WorldLinePoint> predictedWorldLine;

    [Header("视觉效果")]
    public Color startHistoricalColor = Color.gray;
    public Color endHistoricalColor = new Color(0, 0, 0, 1);

    public Color startPredictColor = Color.cyan;
    public Color endPredictColor = new Color(0, 1, 1, 1);

    private HPFloatingPoint currentFloatingPoint;

    [Header("比能")]
    public double H;

    [HideInInspector] public float2 thrust;
    //[HideInInspector] 
    public float torque; // 绕y轴旋转，逆时针为正


    void Awake()
    {
        currentFloatingPoint = GetComponent<HPFloatingPoint>();
        // 初始化数据
        currentFloatingPoint.position = new(x.x, 0, x.y);
    }

    private void Start()
    {
        GRPE = GRPhysicsEngine.Instance;
        properTime = GRPE.globalTime;

        // 初始化 NativeList
        worldLine = new NativeList<WorldLinePoint>(GRWorldLineManager.Instance.maxHistory, Allocator.Persistent);
        GRPE.Register(this);
    }
    private void OnDisable() => GRPE.Unregister(this);

    /// <summary>
    /// 由GRPE调用，用于统一模拟旋转
    /// </summary>
    /// <param name="dt"></param>
    public void UpdateRot(double dt)
    {
        if (GRPE.timeWarp > 10) return;
        float3 eularAngles = transform.eulerAngles;
        transform.eulerAngles = new(eularAngles.x,
                                    eularAngles.y + rotationSpeed * (float)dt,
                                    eularAngles.z);
        rotationSpeed += torque * (float)dt;
    }

    /// <summary> 给GRPO添加力 </summary> <param name="force"></param>
    public void SetForce(float3 force) => thrust = new float2(force.x, force.z);
    /// <summary> 给GRPO添加力矩 </summary> <param name="torque"></param>
    public void SetTorque(float torque) => this.torque = torque;


    #region "世界线数组相关"
    /// <summary>
    /// 每一物理帧结束，由Engine调用此方法记录过去线
    /// </summary>
    /// <param name="t"></param>
    public void RecordWorldLine(double t)
    {
        worldLine.Add(new WorldLinePoint(t, x));

        // 维持长度
        if (worldLine.Length > GRWorldLineManager.Instance.maxHistory)
        {
            worldLine.RemoveAt(0);
        }
    }

    /// <summary>
    /// 更新锚点坐标，防止线和对象脱离
    /// </summary>
    public void UpdateAnchorOnly(double currentTime)
    {
        if (!predictedWorldLine.IsCreated) return;

        WorldLinePoint nowPoint = new WorldLinePoint(currentTime, x);
        if (predictedWorldLine.Length == 0)
            predictedWorldLine.Add(nowPoint);
        else
            predictedWorldLine[0] = nowPoint;
    }

    /// <summary>
    /// 由管理器统一调用的清理函数，用于清理预测线上已经进入过去光锥的部分。pruneCount 是全局统一计算出的过期步数
    /// </summary>
    public void PruneExpiredPoints(int pruneCount)
    {
        if (!predictedWorldLine.IsCreated) return;

        // 批量移除 index 1 之后的过期预测点
        // 保留 index 0 (当前锚点)，删除之后的 pruneCount 个点
        for (int i = 0; i < pruneCount; i++)
        {
            if (predictedWorldLine.Length > 1)
            {
                predictedWorldLine.RemoveAt(1);
            }
            else break;
        }
    }

    /// <summary>
    /// 增量更新预测线，根据当前进度覆盖或追加点（补：目前这个方法还要承担重置预测线列表的功能，屎山代码这一块）
    /// </summary>
    public void UpdatePredictionBatch(NativeArray<WorldLinePoint> batchResults, int batchOffset, int batchSize, int startIdxInList, bool needReset)
    {
        if (!predictedWorldLine.IsCreated || needReset)
            predictedWorldLine = new NativeList<WorldLinePoint>(500, Allocator.Persistent);

        for (int i = 0; i < batchSize; i++)
        {
            // 目标索引（+1是因为索引0被锚点占据了）
            int targetIdx = startIdxInList + i + 1;
            WorldLinePoint newPoint = batchResults[batchOffset + i];

            if (targetIdx < predictedWorldLine.Length)
            {
                // 覆盖旧周期的点
                predictedWorldLine[targetIdx] = newPoint;
            }
            else if (predictedWorldLine.Length < GROrbitPrediction.Instance.predictedPoints + 1)
            {
                // 如果列表还没长到最大长度则追加
                predictedWorldLine.Add(newPoint);
            }
        }
    }
    #endregion

    // 将数据传至浮动原点系统（渲染层更新）
    public void SyncTransform()
    {
        currentFloatingPoint.position = new(x.x, 0, x.y);
        transform.eulerAngles = new(0, transform.eulerAngles.y, 0);
    }

    void OnDestroy()
    {
        if (worldLine.IsCreated) worldLine.Dispose();
    }
}

// 带质量粒子的状态
public struct ObjectStateData
{
    public bool isDynamic;
    public double2 x;
    public double2 u; // 协变速度
    public double mass; // m=0 则不参与多体引力贡献（一般航天器的mass设为0）
    public double energy;
    // 存储dx/dt用于计算隐式牛顿项引力以近似能量守恒
    public double2 dxdt;
    public Metric metric;
    public double deltaProper;
}