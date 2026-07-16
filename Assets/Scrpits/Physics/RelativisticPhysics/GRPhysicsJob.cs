using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[BurstCompile]
public struct GRPhysicsJob : IJob
{
    public NativeArray<ObjectStateData> States;
    public double2 aM; // SI 制
    public double G, c;
    public double dt; // SI制
    public int subStepCount; // 单帧的积分步数

    double c2;
    double2 aM_geom;
    Metric metric;

    public void Execute()
    {
        // 常量的几何制转换放到积分开始前避免占用多余CPU
        c2 = c * c;
        double M_geom = aM.y * G / c2;
        double a_geom = aM.x;
        aM_geom = new double2(a_geom, M_geom);


        int count = States.Length;
        NativeArray<ObjectStateData> nextStates = new NativeArray<ObjectStateData>(count, Allocator.Temp);
        for (int step = 0; step < subStepCount; step++)
        {
            // 对每个物体进行数值积分 (1:1 搬运你之前的 Execute 逻辑)
            for (int i = 0; i < count; i++)
            {
                nextStates[i] = SolveSingleObject(i, States, dt);
            }

            States.CopyFrom(nextStates);
        }

        nextStates.Dispose();
    }

    private ObjectStateData SolveSingleObject(int index, NativeArray<ObjectStateData> currentStates, double dt)
    {
        ObjectStateData s = currentStates[index];

        // y 向量定义为 [x.x, x.y, u.x, u.y] SI制
        double4 y_n = new double4(s.x, s.u);

        // 使用显式步长预测初值，加快牛顿法收敛 SI制
        CalculateDerivatives(index, s.x, s.u, currentStates, out double2 dxdt, out double2 dudt);
        double4 y_next = y_n + new double4(dxdt, dudt) * dt;

        // 牛顿法迭代求解隐式中点方程 SI制
        // F(y_next) = y_next - y_n - dt * f((y_n + y_next)/2) = 0
        double tolerance = 1e-13; // 容差

        double energy = 0;
        double u0 = 1;

        for (int iter = 0; iter < 10; iter++)
        {
            double4 y_mid = (y_n + y_next) * 0.5;

            // 计算当前残差 F
            CalculateDerivatives(index, y_mid.xy, y_mid.zw, currentStates, out dxdt, out dudt, out energy, out metric, out u0);
            double4 f_mid = new double4(dxdt, dudt);
            double4 F = y_next - y_n - f_mid * dt;

            // 检查是否收敛
            if (math.all(math.abs(F) < tolerance)) break;

            // 计算数值雅可比矩阵 J = I - (dt/2) * df/dy
            double4x4 J = ComputeJacobian(index, y_mid.xy, y_mid.zw, currentStates, dt);

            // 解线性方程 J * delta_y = -F
            double4 delta_y = math.mul(math.inverse(J), -F);

            y_next += delta_y;
        }

        s.x = y_next.xy;
        s.u = y_next.zw;
        s.energy = energy;
        s.dxdt = dxdt;

        s.metric = metric;
        s.deltaProper += dt / u0; // 要在Engine层对deltaProper进行清零
        return s;
    }

    // 使用中心差分法计算雅可比矩阵
    private double4x4 ComputeJacobian(int index, double2 x, double2 u, NativeArray<ObjectStateData> states, double dt)
    {
        double h = 1e-7; // 差分步长
        double4x4 dfdy = double4x4.zero;

        // 分别对 [x.x, x.y, u.x, u.y] 施加扰动
        for (int i = 0; i < 4; i++)
        {
            double4 y_pos = new double4(x, u);
            y_pos[i] += h;
            CalculateDerivatives(index, y_pos.xy, y_pos.zw, states, out double2 dxdt_p, out double2 dudt_p);

            double4 y_neg = new double4(x, u);
            y_neg[i] -= h;
            CalculateDerivatives(index, y_neg.xy, y_neg.zw, states, out double2 dxdt_n, out double2 dudt_n);

            // (f(y+h) - f(y-h)) / 2h
            double4 column = (new double4(dxdt_p, dudt_p) - new double4(dxdt_n, dudt_n)) / (2.0 * h);

            if (i == 0) dfdy.c0 = column;
            else if (i == 1) dfdy.c1 = column;
            else if (i == 2) dfdy.c2 = column;
            else dfdy.c3 = column;
        }

        // J = I - (dt/2) * df/dy
        return double4x4.identity - dfdy * (dt * 0.5);
    }

    private void CalculateDerivatives(int currentIndex, double2 x_SI, double2 u_SI, NativeArray<ObjectStateData> states, out double2 dxdt_SI, out double2 dudt_SI, out double energy, out Metric metric, out double u0)
    {
        // SI制物理量转为几何制用于测地线方程的计算，即使G = c = 1
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
        u0 = math.sqrt(math.dot(u_geom, math.mul(gammaInv, u_geom)) + epsilon) / alpha;

        double2 dxdt_geom = math.mul(gammaInv, u_geom) / u0 - beta;

        double2 dudt_geom = -u0 * gradAlpha * alpha
                            + new double2(math.dot(u_geom, gradBetaX), math.dot(u_geom, gradBetaY))
                            - new double2(math.dot(u_geom, math.mul(gradGammaInvX, u_geom)),
                                          math.dot(u_geom, math.mul(gradGammaInvY, u_geom))) / (2 * u0);

        // 转回SI制
        dxdt_SI = dxdt_geom * c;
        dudt_SI = dudt_geom * c2;
        // 多体牛顿项（平均值法近似能量守恒）
        for (int i = 0; i < states.Length; i++)
        {
            if (i == currentIndex) continue;
            ObjectStateData other = states[i];
            if (other.mass <= 0) continue;


            double2 v_other = other.dxdt;
            double2 x_other_mid = other.x + v_other * (dt * 0.5);
            double2 diff = x_other_mid - x_SI;

            double r2_other = math.lengthsq(diff);
            if (r2_other < 1e-6) continue;

            dudt_SI += diff / math.sqrt(r2_other) * (G * other.mass / r2_other);
        }



        double h_geom = alpha * alpha * u0 - math.dot(beta, u_geom);

        double e_gr = h_geom * c * c; // SI 比能 (J/kg)

        // 计算牛顿势能项
        double potentialV = 0;
        for (int i = 0; i < states.Length; i++)
        {
            if (i == currentIndex || states[i].mass <= 0) continue;

            double dist = math.distance(states[i].x, x_SI);

            if (dist > 1e-6)
                potentialV -= G * states[i].mass / dist;
        }

        // 在多体势能求和中，V_ij 会被 i 和 j 各算一次，所以势能项要除以 2 避免重复统计
        energy = e_gr + 0.5 * potentialV;

        // 外部要使用的度规分量，所以这里计算完的度规也传出去
        metric = new Metric
        {
            alpha = alpha,
            beta_i = beta,
            gamma_ij = gamma
        };
    }
    private void CalculateDerivatives(int currentIndex, double2 x_SI, double2 u_SI, NativeArray<ObjectStateData> states, out double2 dxdt_SI, out double2 dudt_SI)
    {
        // SI制物理量转为几何制用于测地线方程的计算，即使G = c = 1
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
        for (int i = 0; i < states.Length; i++)
        {
            if (i == currentIndex) continue;
            ObjectStateData other = states[i];
            if (other.mass <= 0) continue;


            double2 v_other = other.dxdt;
            double2 x_other_mid = other.x + v_other * (dt * 0.5);
            double2 diff = x_other_mid - x_SI;

            double r2_other = math.lengthsq(diff);
            if (r2_other < 1e-6) continue;

            dudt_SI += diff / math.sqrt(r2_other) * (G * other.mass / r2_other);
        }
    }
}

/// <summary>
/// ADM分解下的时空度规（几何制）
/// </summary>
public struct Metric
{
    public double alpha;
    public double2 beta_i; // 注意这里的 beta_i 和 RelativityMath 里一样实际上是指逆变矢量 bata^i，但是方法名不能使用 ^ 符号，所以只能写下划线了
    public double2x2 gamma_ij;
}