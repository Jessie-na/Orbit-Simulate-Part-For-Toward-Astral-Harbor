using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class GROrbitPrediction : MonoBehaviour
{
    public static GROrbitPrediction Instance;
    public bool enablePrediction = false;
    public int predictedPoints = 500;
    public double predictionTimeStep = 50;
    public int stepsPerFrame = 50;

    [Header("重置设置")]
    private float resetTimer = 0;
    private int currentComputedStep = 0;

    private NativeArray<ObjectStateData> virtualSystem;

    private List<GRPhysicsObject> managedObjects = new List<GRPhysicsObject>();

    private double G, c, a, M;
    bool needReset;

    void Awake()
    {
        Instance = this;
        // 传入物理引擎数据
        var physicsEngine = GRPhysicsEngine.Instance;
        managedObjects = physicsEngine.managedObjects; // 后续有游戏内变更物理对象的话得在这里一同更新
        G = physicsEngine.G;
        c = physicsEngine.c;
        a = physicsEngine.a;
        M = physicsEngine.M;

        // 用于检测运行时是否有更改这些数值，如果被改的话需要把预测线列表进行重置，否则直接覆写会出bug（具体检测逻辑在LateUpdate里）
        lastPredictedPoints = predictedPoints;
        lastPredictionTimeStep = predictionTimeStep;
    }

    private double snapshotBaseTime; // 记录当前这一轮预测开始时的坐标时

    int lastPredictedPoints;
    double lastPredictionTimeStep;

    void LateUpdate()
    {
        int count = managedObjects.Count;
        if (count == 0) return;

        double currentRealTime = GRPhysicsEngine.Instance.globalTime;

        // 检测运行时是否有更改这些数值，如果被改的话需要把预测线列表进行重置，否则直接覆写会出bug
        needReset = false;
        if (lastPredictedPoints != predictedPoints || lastPredictionTimeStep != predictionTimeStep)
        {
            lastPredictedPoints = predictedPoints;
            lastPredictionTimeStep = predictionTimeStep;
            needReset = true;
        }

        if (virtualSystem.IsCreated)
        {
            // 计算从快照开始到现在，已经过去了多少个完整的 predictionTimeStep
            double timeSinceSnapshot = currentRealTime - snapshotBaseTime;
            int stepsToPrune = (int)math.floor(timeSinceSnapshot / predictionTimeStep);

            if (stepsToPrune > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (managedObjects[i] != null)
                        managedObjects[i].PruneExpiredPoints(stepsToPrune);
                }
                // 步进快照基准时间，保持与删除的点数同步
                snapshotBaseTime += stepsToPrune * predictionTimeStep;
                // 当前已计算的步数进度也要减去删除的步数，防止 UpdatePredictionBatch 覆写错位
                currentComputedStep -= stepsToPrune;
                if (currentComputedStep < 0) currentComputedStep = 0;
            }
        }

        // 更新所有物体的锚点
        for (int i = 0; i < count; i++)
        {
            if (managedObjects[i] != null)
                managedObjects[i].UpdateAnchorOnly(currentRealTime);
        }

        // 重置循环
        if ((currentComputedStep >= predictedPoints) || !virtualSystem.IsCreated)
        {
            resetTimer = 0;
            currentComputedStep = 0;
            snapshotBaseTime = currentRealTime; // 锁定新的快照时间点

            if (virtualSystem.IsCreated) virtualSystem.Dispose();
            virtualSystem = new NativeArray<ObjectStateData>(GRPhysicsEngine.Instance.statesInput, Allocator.Persistent);
        }

        int remainingSteps = predictedPoints - currentComputedStep;
        if (remainingSteps <= 0) return;
        int batchSize = math.min(stepsPerFrame, remainingSteps);

        // 预测世界线的存储
        NativeArray<WorldLinePoint> batchResults = new NativeArray<WorldLinePoint>(count * batchSize, Allocator.TempJob);

        // 调度预测 Job
        var predictJob = new GRPredictionJob
        {
            aM = new double2(a, M),
            G = G,
            c = c,
            VirtualSystem = virtualSystem,
            PredictionResults = batchResults,
            steps = batchSize,
            timeStep = predictionTimeStep,
            // 传入基于当前进度的坐标时偏移
            globalTime = GRPhysicsEngine.Instance.globalTime + (currentComputedStep * predictionTimeStep)
        };

        predictJob.Schedule().Complete();

        for (int i = 0; i < count; i++)
        {
            if (managedObjects[i] != null)
            {
                managedObjects[i].UpdatePredictionBatch(batchResults, i * batchSize, batchSize, currentComputedStep, needReset);
            }
        }

        currentComputedStep += batchSize;
        batchResults.Dispose();
    }


    void OnDestroy()
    {
        if (virtualSystem.IsCreated) virtualSystem.Dispose();
    }

}

[BurstCompile]
public struct GRPredictionJob : IJob
{
    public NativeArray<ObjectStateData> VirtualSystem;
    public NativeArray<WorldLinePoint> PredictionResults;

    // 参数
    public double2 aM;
    public double G, c;
    public int steps; // 步长数
    public double timeStep; // 单个步长
    public double globalTime; // 预测的起点时间

    public void Execute()
    {
        int count = VirtualSystem.Length;
        double simulatedTime = globalTime;

        // 准备一个缓冲区，存储下一步的结果，实现类似OutputData的功能
        NativeArray<ObjectStateData> nextStepBuffer = new NativeArray<ObjectStateData>(count, Allocator.Temp);

        for (int s = 0; s < steps; s++)
        {
            // 模拟物理 Job 的执行过程
            
            for (int i = 0; i < count; i++)
            {
                nextStepBuffer[i] = SolveObjectStep(i, VirtualSystem[i], VirtualSystem);
            }

            VirtualSystem.CopyFrom(nextStepBuffer);

            simulatedTime += timeStep;

            for (int i = 0; i < count; i++)
            {
                int outIdx = i * steps + s;
                PredictionResults[outIdx] = new WorldLinePoint(simulatedTime, VirtualSystem[i].x);
            }
        }

        nextStepBuffer.Dispose();
    }

    // 这里与PhysicsJob内容基本相同,不过只使用5次牛顿法迭代
    private ObjectStateData SolveObjectStep(int index, ObjectStateData s, NativeArray<ObjectStateData> currentSystem)
    {
        double4 y_n = new double4(s.x, s.u);

        // 1. Predictor
        CalculateDerivatives(index, s.x, s.u, currentSystem, out double2 dxdt, out double2 dudt);
        double4 y_next = y_n + new double4(dxdt, dudt) * timeStep;

        // 2. Newton-Raphson Corrector
        double tolerance = 1e-13;
        for (int iter = 0; iter < 10; iter++)
        {
            double4 y_mid = (y_n + y_next) * 0.5;

            CalculateDerivatives(index, y_mid.xy, y_mid.zw, currentSystem, out dxdt, out dudt);
            double4 f_mid = new double4(dxdt, dudt);
            double4 F = y_next - y_n - f_mid * timeStep;

            if (math.all(math.abs(F) < tolerance)) break;

            double4x4 J = ComputeJacobian(index, y_mid.xy, y_mid.zw, currentSystem);
            y_next += math.mul(math.inverse(J), -F);
        }

        s.x = y_next.xy;
        s.u = y_next.zw;
        s.dxdt = dxdt; // 更新导数供下一步其他物体使用
        return s;
    }


    // 使用中心差分法计算雅可比矩阵
    private double4x4 ComputeJacobian(int index, double2 x, double2 u,NativeArray<ObjectStateData> snapshot)
    {
        double h = 1e-7; // 差分步长
        double4x4 dfdy = double4x4.zero;

        // 分别对 [x.x, x.y, u.x, u.y] 施加扰动
        for (int i = 0; i < 4; i++)
        {
            double4 y_pos = new double4(x, u);
            y_pos[i] += h;
            CalculateDerivatives(index, y_pos.xy, y_pos.zw, snapshot, out double2 dxdt_p, out double2 dudt_p);

            double4 y_neg = new double4(x, u);
            y_neg[i] -= h;
            CalculateDerivatives(index, y_neg.xy, y_neg.zw, snapshot, out double2 dxdt_n, out double2 dudt_n);

            // (f(y+h) - f(y-h)) / 2h
            double4 column = (new double4(dxdt_p, dudt_p) - new double4(dxdt_n, dudt_n)) / (2.0 * h);
            
            if (i == 0) dfdy.c0 = column;
            else if (i == 1) dfdy.c1 = column;
            else if (i == 2) dfdy.c2 = column;
            else dfdy.c3 = column;
        }

        // J = I - (dt/2) * df/dy
        return double4x4.identity - dfdy * (timeStep * 0.5);
    }
    
    private void CalculateDerivatives(int currentIndex, double2 x_SI, double2 u_SI,NativeArray<ObjectStateData> snapshot, out double2 dxdt_SI, out double2 dudt_SI)
    {
        // SI制物理量转为几何制用于测地线方程的计算，即G = c = 1
        double c2 = c * c;
        double M_geom = aM.y * G / c2;
        double a_geom = aM.x;
        double2 aM_geom = new double2(a_geom, M_geom);

        double2 x_geom = x_SI;
        double2 u_geom = u_SI / c;
    
        double r2 = math.lengthsq(x_geom);
        double r = math.sqrt(r2);
        
        // 辅助量的计算
        double D = RelativityMath.comput_D(aM_geom, r2, r);
        double dD = RelativityMath.comput_dD(aM_geom, r2, r);
        double delta = RelativityMath.comput_Delta(aM_geom, r2, r);
        double dDelta = RelativityMath.comput_dDelta(aM_geom, r);
        double G_Aux = RelativityMath.comput_G(aM_geom, r, D); 
        double dG = RelativityMath.comput_dG(aM_geom, r, D, dD);
        double H = RelativityMath.comput_H(delta);
        double dH = RelativityMath.comput_dH(delta, dDelta);
        double K = RelativityMath.comput_K(D, r2, r2 * r2 * r2);
        double dK = RelativityMath.comput_dK(D, dD, r2, r, r2 * r2 * r2);

        double alpha = RelativityMath.comput_Alpha(r2, D, delta);
        double2 beta = RelativityMath.comput_Beta_i(x_geom, G_Aux);
        double2x2 gamma = RelativityMath.comput_Gamma_ij(x_geom, H, K);
        double2x2 gammaInv = RelativityMath.comput_GammaInv_ij(gamma);

        double2 gradAlpha = new double2(RelativityMath.comput_xPartialAlpha(x_geom, alpha, delta, dDelta, D, dD, r),
                                        RelativityMath.comput_yPartialAlpha(x_geom, alpha, delta, dDelta, D, dD, r));

        double2 gradBetaX = RelativityMath.comput_xPartialBeta_i(x_geom, G_Aux, dG, r);
        double2 gradBetaY = RelativityMath.comput_yPartialBeta_i(x_geom, G_Aux, dG, r);

        double2x2 gradGammaX = RelativityMath.comput_xPartialGamma_ij(x_geom, H, dH, K, dK, r);
        double2x2 gradGammaY = RelativityMath.comput_yPartialGamma_ij(x_geom, H, dH, K, dK, r);

        double2x2 gradGammaInvX = RelativityMath.comput_xPartialGammaInv_ij(gammaInv, gradGammaX);
        double2x2 gradGammaInvY = RelativityMath.comput_yPartialGammaInv_ij(gammaInv, gradGammaY);

        double epsilon = 1.0; // 带质量粒子取1，光子取0
        double u0 = math.sqrt(math.dot(u_geom, math.mul(gammaInv, u_geom)) + epsilon) / alpha;

        double2 dxdt_geom = math.mul(gammaInv, u_geom) / u0 - beta;

        double2 dudt_geom = -u0 * gradAlpha * alpha
                            + new double2(math.dot(u_geom, gradBetaX), math.dot(u_geom, gradBetaY))
                            - new double2(math.dot(u_geom, math.mul(gradGammaInvX, u_geom)),
                                          math.dot(u_geom, math.mul(gradGammaInvY, u_geom))) / (2 * u0);

        // 转回SI制
        dxdt_SI = dxdt_geom * c;
        dudt_SI = dudt_geom * c2;

        // 多体牛顿项（平均值法近似能量守恒）
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (i == currentIndex) continue;
            
            ObjectStateData other = snapshot[i];
            if (other.mass <= 0) continue;


            double2 v_other = other.dxdt;
            double2 x_other_mid = other.x + v_other * (timeStep * 0.5);
            double2 diff = x_other_mid - x_SI;

            double r2_other = math.lengthsq(diff);
            if (r2_other < 1e-6) continue;

            dudt_SI += diff / math.sqrt(r2_other) * (G * other.mass / r2_other);
        }
    }
}
