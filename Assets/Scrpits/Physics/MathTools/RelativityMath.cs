namespace Unity.Mathematics
{
    public class RelativityMath
    {
        // ------------------------------------------------------测地线方程展开成一阶微分方程组---------------------------------------------------------------



        //--------------------------------------------------------------------------------------------------
        //---------------------------------ADM分解下的度规各分量及其微分--------------------------------------
        //--------------------------------------------------------------------------------------------------

        //---------------------------------------------辅助量-----------------------------------------------

        #region 辅助量（简化形式）

        // 辅助量 (D)
        public static double comput_D(double2 aM, double r2, double r)
        {
            double a = aM.x;
            double M = aM.y;
            double a2 = a * a;

            double D = r2 * r2 + a2 * r2 + 2 * M * a2 * r;
            return D;
        }
        // 辅助量 d(D)/dr
        public static double comput_dD(double2 aM, double r2, double r)
        {
            double a = aM.x;
            double M = aM.y;
            double a2 = a * a;

            double dD = 4 * r2 * r + 2 * a2 * r + 2 * M * a2;
            return dD;
        }

        // 辅助量 (delta)
        public static double comput_Delta(double2 aM, double r2, double r)
        {
            double a = aM.x;
            double M = aM.y;

            double delta = r2 - 2 * M * r + a * a;
            return delta;
        }
        // 辅助量 d(delta)/dr
        public static double comput_dDelta(double2 aM, double r)
        {
            double M = aM.y;

            double dDelta = 2 * (r - M);
            return dDelta;
        }

        // 辅助量 (G)
        public static double comput_G(double2 aM, double r, double D)
        {
            double a = aM.x;
            double M = aM.y;

            double G = a * 2 * M * r / D;
            return G;
        }
        // 辅助量 d(G)/dr
        public static double comput_dG(double2 aM, double r, double D, double dD)
        {
            double a = aM.x;
            double M = aM.y;

            double dG = a * (2 * M / D - 2 * M * r * dD / (D * D));
            return dG;
        }

        // 辅助量 (H)
        public static double comput_H(double delta)
        {
            double H = 1 / delta;
            return H;
        }
        // 辅助量 d(H)/dr
        public static double comput_dH(double delta, double dDelta)
        {
            double dH = -dDelta / (delta * delta);
            return dH;
        }

        // 辅助量 (K)
        public static double comput_K(double D, double r2, double r6)
        {
            double K = D / r6;
            return K;
        }
        // 辅助量 d(K)/dr
        public static double comput_dK(double D, double dD, double r2, double r, double r6)
        {
            double dK = dD / r6 - 6 * D / (r6 * r);
            return dK;
        }
        #endregion

        #region 辅助量（独立形式）
        /// 辅助量 (D)
        public static double comput_D(double2 xy, double2 aM)
        {
            double a = aM.x;
            double M = aM.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double a2 = a * a;

            double D = r2 * r2 + a2 * r2 + 2 * M * a2 * r;
            return D;
        }
        // 辅助量 d(D)/dr
        public static double comput_dD(double2 xy, double2 aM)
        {
            double a = aM.x;
            double M = aM.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double a2 = a * a;

            double dD = 4 * r2 * r + 2 * a2 * r + 2 * M * a2;
            return dD;
        }

        // 辅助量 (delta)
        public static double comput_Delta(double2 xy, double2 aM)
        {
            double a = aM.x;
            double M = aM.y;

            double r2 = math.lengthsq(xy);;

            double delta = r2 - 2 * M * math.sqrt(r2) + a * a;
            return delta;
        }
        // 辅助量 d(delta)/dr
        public static double comput_dDelta(double2 xy, double2 aM)
        {
            double M = aM.y;

            double r = math.length(xy);

            double dDelta = 2 * (r - M);
            return dDelta;
        }

        // 辅助量 (G)
        public static double comput_G(double2 xy, double2 aM)
        {
            double a = aM.x;
            double M = aM.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double D = comput_D(aM, r2, r);

            double G = a * (2 * M * r) / D;
            return G;
        }
        // 辅助量 d(G)/dr
        public static double comput_dG(double2 xy, double2 aM)
        {
            double a = aM.x;
            double M = aM.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double dG = a * (2 * M / D - 2 * M * r * dD / (D * D));
            return dG;
        }

        // 辅助量 (H)
        public static double comput_H(double2 xy,double2 aM)
        {
            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double delta = comput_Delta(aM, r2, r);

            double H = 1 / delta;
            return H;
        }
        // 辅助量 d(H)/dr
        public static double comput_dH(double2 xy, double2 aM)
        {
            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double dDelta = comput_dDelta(aM, r);
            double delta = comput_Delta(aM, r2, r);

            double dH = -dDelta / (delta * delta);
            return dH;
        }

        // 辅助量 (K)
        public static double comput_K(double2 xy, double2 aM)
        {
            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double r6 = r2 * r2 * r2;
            double D = comput_D(aM, r2, r);

            double K = D / r6;
            return K;
        }
        // 辅助量 d(K)/dr
        public static double comput_dK(double2 xy,double2 aM)
        {
            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double r6 = r2 * r2 * r2;
            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double dK = dD / r6 - 6 * D / (r6 * r);
            return dK;
        }
        #endregion

        //-------------------------------------------度规各分量---------------------------------------------

        #region 度规各分量（简化形式）
        // 度规时间分量 (alpha)
        public static double comput_Alpha(double r2, double D, double delta)
        {
            double alpha = math.sqrt(delta * r2 / D);
            return alpha;
        }

        // 度规时空偏移分量 (beta_i)
        // 注意这里的 beta_i 实际上是指逆变矢量 bata^i，但是方法名不能使用^符号，所以只能写下划线了
        public static double2 comput_Beta_i(double2 xy, double G)
        {
            double x = xy.x;
            double y = xy.y;

            double beta_x = y * G;
            double beta_y = -x * G;

            double2 beta_i = new double2(beta_x, beta_y);
            return beta_i;
        }

        // 度规空间分量 (gamma_ij)
        public static double2x2 comput_Gamma_ij(double2 xy, double H, double K)
        {
            double x = xy.x;
            double y = xy.y;

            double gamma_xx = x * x * H + y * y * K;
            double gamma_xy = x * y * (H - K);
            double gamma_yy = y * y * H + x * x * K;

            double2x2 gamma_ij = new double2x2(gamma_xx, gamma_xy,
                                               gamma_xy, gamma_yy);
            return gamma_ij;
        }

        // gamma^ij（gamma_ij的逆矩阵）
        public static double2x2 comput_GammaInv_ij(double2x2 gamma_ij)
        {
            return math.inverse(gamma_ij);
        }
        #endregion

        #region 度规各分量（独立形式）
        // 度规时间分量 (alpha)
        public static double comput_Alpha(double2 xy, double2 aM)
        {
            double D = comput_D(xy, aM);
            double delta = comput_Delta(xy, aM);
            double r2 = math.lengthsq(xy);

            double alpha = math.sqrt(delta * r2 / D);
            return alpha;
        }

        // 度规时空偏移分量 (beta_i)
        public static double2 comput_Beta_i(double2 xy, double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double D = comput_D(aM, r2, r);
            double G = comput_G(aM, r, D);

            double beta_x = y * G;
            double beta_y = -x * G;

            double2 beta_i = new double2(beta_x, beta_y);
            return beta_i;
        }

        // 度规空间分量 (gamma_ij)
        public static double2x2 comput_Gamma_ij(double2 xy, double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double r6 = r2 * r2 * r2;

            double D = comput_D(aM, r2, r);
            double delta = comput_Delta(aM, r2, r);

            double H = comput_H(delta);
            double K = comput_K(D, r2, r6);

            double gamma_xx = x * x * H + y * y * K;
            double gamma_xy = x * y * (H - K);
            double gamma_yy = y * y * H + x * x * K;

            double2x2 gamma_ij = new double2x2(gamma_xx, gamma_xy,
                                               gamma_xy, gamma_yy);
            return gamma_ij;
        }

        // gamma^ij（gamma_ij的逆矩阵）
        public static double2x2 comput_GammaInv_ij(double2 xy, double2 aM)
        {
            double H = comput_H(xy, aM);
            double K = comput_K(xy, aM);
            double2x2 gamma_ij = comput_Gamma_ij(xy, H, K);
            double detGamma_ij = math.determinant(gamma_ij);

            return gamma_ij / detGamma_ij;
        }
        
        #endregion


        //----------------------------------------度规各分量的微分---------------------------------------------

        #region 度规各分类的微分（简化形式）
        // 度规时间分量 d(alpha)
        public static double comput_xPartialAlpha(double2 xy, double alpha, double delta, double dDelta, double D, double dD, double r)
        {
            double x = xy.x;

            double xPartialAlpha = alpha * x / (2 * r) * (dDelta / delta + 2 / r - dD / D);
            return xPartialAlpha;
        }
        public static double comput_yPartialAlpha(double2 xy, double alpha, double delta, double dDelta, double D, double dD, double r)
        {            
            double y = xy.y;
            
            double yPartialAlpha = alpha * y / (2 * r) * (dDelta / delta + 2 / r - dD / D);
            return yPartialAlpha;
        }

        // 度规时空偏移分量 d(beta_i)
        public static double2 comput_xPartialBeta_i(double2 xy, double G, double dG, double r)
        {
            double x = xy.x;
            double y = xy.y;

            double xPartialBeta_x = y * dG * x / r;
            double xPartialBeta_y = -G - x * dG * x / r;

            double2 xPartialBeta_i = new double2(xPartialBeta_x, xPartialBeta_y);
            return xPartialBeta_i;
        }
        public static double2 comput_yPartialBeta_i(double2 xy, double G, double dG, double r)
        {
            double x = xy.x;
            double y = xy.y;            

            double yPartialBeta_x = G + y * dG * y / r;
            double yPartialBeta_y = -x * dG * y / r;

            double2 yPartialBeta_i = new double2(yPartialBeta_x, yPartialBeta_y);
            return yPartialBeta_i;
        }

        // 度规空间分量 d(gamma_ij)
        public static double2x2 comput_xPartialGamma_ij(double2 xy, double H, double dH, double K, double dK, double r)
        {
            double x = xy.x;
            double y = xy.y;
            
            double r_x = x / r;            

            double xPartialGamma_xx = 2 * x * H + (x * x * dH + y * y * dK) * r_x;
            double xPartialGamma_xy = y * (H - K) + x * y * (dH - dK) * r_x;
            double xPartialGamma_yy = 2 * x * K + (x * x * dK + y * y * dH) * r_x;

            double2x2 xPartialGamma_ij = new double2x2(xPartialGamma_xx, xPartialGamma_xy, 
                                                       xPartialGamma_xy, xPartialGamma_yy);
            return xPartialGamma_ij;
        }
        public static double2x2 comput_yPartialGamma_ij(double2 xy, double H, double dH, double K, double dK, double r)
        {
            double x = xy.x;
            double y = xy.y;

            double r_y = y / r;

            double yPartialGamma_xx = 2 * y * K + (x * x * dH + y * y * dK) * r_y;
            double yPartialGamma_xy = x * (H - K) + x * y * (dH - dK) * r_y;
            double yPartialGamma_yy = 2 * y * H + (x * x * dK + y * y * dH) * r_y;

            return new double2x2(yPartialGamma_xx, yPartialGamma_xy,
                                 yPartialGamma_xy, yPartialGamma_yy);
        }
        // d(gamma^ij)（gamma_ij逆矩阵的微分）
        public static double2x2 comput_xPartialGammaInv_ij(double2x2 gammaInv_ij, double2x2 xPartialGamma_ij)
        {
            double gxx = gammaInv_ij[0][0];
            double gxy = gammaInv_ij[0][1];
            double gyy = gammaInv_ij[1][1];

            double dxx = xPartialGamma_ij[0][0];
            double dxy = xPartialGamma_ij[0][1];
            double dyy = xPartialGamma_ij[1][1];

            double xPartialGammaInv_xx = -(gxx * gxx * dxx + 2 * gxx * gxy * dxy + gxy * gxy * dyy);
            double xPartialGammaInv_xy = -(gxx * gxy * dxx + (gxx * gyy + gxy * gxy) * dxy + gxy * gyy * dyy);
            double xPartialGammaInv_yy = -(gxy * gxy * dxx + 2 * gxy * gyy * dxy + gyy * gyy * dyy);

            return new double2x2(xPartialGammaInv_xx, xPartialGammaInv_xy,
                                 xPartialGammaInv_xy, xPartialGammaInv_yy);
        }
        public static double2x2 comput_yPartialGammaInv_ij(double2x2 gammaInv_ij, double2x2 yPartialGamma_ij)
        {
            double gxx = gammaInv_ij[0][0];
            double gxy = gammaInv_ij[0][1];
            double gyy = gammaInv_ij[1][1];

            double dxx = yPartialGamma_ij[0][0];
            double dxy = yPartialGamma_ij[0][1];
            double dyy = yPartialGamma_ij[1][1];

            double yPartialGammaInv_xx = -(gxx * gxx * dxx + 2 * gxx * gxy * dxy + gxy * gxy * dyy);
            double yPartialGammaInv_xy = -(gxx * gxy * dxx + (gxx * gyy + gxy * gxy) * dxy + gxy * gyy * dyy);
            double yPartialGammaInv_yy = -(gxy * gxy * dxx + 2 * gxy * gyy * dxy + gyy * gyy * dyy);

            return new double2x2(yPartialGammaInv_xx, yPartialGammaInv_xy,
                                 yPartialGammaInv_xy, yPartialGammaInv_yy);
        }
        #endregion

        #region 度规各分量的微分（独立形式）
        // 度规时间分量 d(alpha)
        public static double comput_xPartialAlpha(double2 xy, double2 aM)
        {
            double x = xy.x;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double delta = comput_Delta(aM, r2, r);
            double dDelta = comput_dDelta(aM, r);

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double alpha = comput_Alpha(r2, D, delta);

            double xPartialAlpha = alpha * x / (2 * r) * (dDelta / delta + 2 / r - dD / D);
            return xPartialAlpha;
        }
        public static double comput_yPartialAlpha(double2 xy, double2 aM)
        {
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double delta = comput_Delta(aM, r2, r);
            double dDelta = comput_dDelta(aM, r);

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double alpha = comput_Alpha(r2, D, delta);

            double yPartialAlpha = alpha * y / (2 * r) * (dDelta / delta + 2 / r - dD / D);
            return yPartialAlpha;
        }

        // 度规时空偏移分量 d(beta_i)
        public static double2 comput_xPartialBeta_i(double2 xy, double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double G = comput_G(aM, r, D);
            double dG = comput_dG(aM, r, D, dD);

            double xPartialBeta_x = y * dG * x / r;
            double xPartialBeta_y = -G - x * dG * x / r;

            double2 xPartialBeta_i = new double2(xPartialBeta_x, xPartialBeta_y);
            return xPartialBeta_i;
        }
        public static double2 comput_yPartialBeta_i(double2 xy, double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);

            double G = comput_G(aM, r, D);
            double dG = comput_dG(aM, r, D, dD);

            double yPartialBeta_x = G + y * dG * y / r;
            double yPartialBeta_y = -x * dG * y / r;

            double2 yPartialBeta_i = new double2(yPartialBeta_x, yPartialBeta_y);
            return yPartialBeta_i;
        }

        // 度规空间分量 d(gamma_ij)
        public static double2x2 comput_xPartialGamma_ij(double2 xy,double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double r6 = r2 * r2 * r2;
            double r_x = x / r;

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);
            double delta = comput_Delta(aM, r2, r);
            double dDelta = comput_dDelta(aM, r);

            double H = comput_H(delta);
            double dH = comput_dH(delta, dDelta);
            double K = comput_K(D, r2, r6);
            double dK = comput_dK(D, dD, r2, r, r6);

            double xPartialGamma_xx = 2 * x * H + (x * x * dH + y * y * dK) * r_x;
            double xPartialGamma_xy = y * (H - K) + x * y * (dH - dK) * r_x;
            double xPartialGamma_yy = 2 * x * K + (x * x * dK + y * y * dH) * r_x;

            double2x2 xPartialGamma_ij = new double2x2(xPartialGamma_xx, xPartialGamma_xy, 
                                                       xPartialGamma_xy, xPartialGamma_yy);
            return xPartialGamma_ij;
        }
        public static double2x2 comput_yPartialGamma_ij(double2 xy, double2 aM)
        {
            double x = xy.x;
            double y = xy.y;

            double r2 = math.lengthsq(xy);
            double r = math.sqrt(r2);
            double r6 = r2 * r2 * r2;
            double r_y = y / r;

            double D = comput_D(aM, r2, r);
            double dD = comput_dD(aM, r2, r);
            double delta = comput_Delta(aM, r2, r);
            double dDelta = comput_dDelta(aM, r);

            double H = comput_H(delta);
            double dH = comput_dH(delta, dDelta);
            double K = comput_K(D, r2, r6);
            double dK = comput_dK(D, dD, r2, r, r6);

            double yPartialGamma_xx = 2 * y * K + (x * x * dH + y * y * dK) * r_y;
            double yPartialGamma_xy = x * (H - K) + x * y * (dH - dK) * r_y;
            double yPartialGamma_yy = 2 * y * H + (x * x * dK + y * y * dH) * r_y;

            double2x2 yPartialGamma_ij = new double2x2(yPartialGamma_xx, yPartialGamma_xy,
                                                       yPartialGamma_xy, yPartialGamma_yy);
            return yPartialGamma_ij;
        }
        // d(gamma^ij)（gamma_ij逆矩阵的微分）
        public static double2x2 comput_xPartialGammaInv_ij(double2 xy, double2 aM)
        {
            double2x2 gamma_ij = comput_Gamma_ij(xy, aM);
            double2x2 gammaInv_ij = comput_GammaInv_ij(gamma_ij);
            double2x2 xPartialGamma_ij = comput_xPartialGamma_ij(xy, aM);

            double gxx = gammaInv_ij[0][0];
            double gxy = gammaInv_ij[0][1];
            double gyy = gammaInv_ij[1][1];

            double dxx = xPartialGamma_ij[0][0];
            double dxy = xPartialGamma_ij[0][1];
            double dyy = xPartialGamma_ij[1][1];

            double xPartialGammaInv_xx = -(gxx * gxx * dxx + 2 * gxx * gxy * dxy + gxy * gxy * dyy);
            double xPartialGammaInv_xy = -(gxx * gxy * dxx + (gxx * gyy + gxy * gxy) * dxy + gxy * gyy * dyy);
            double xPartialGammaInv_yy = -(gxy * gxy * dxx + 2 * gxy * gyy * dxy + gyy * gyy * dyy);

            return new double2x2(xPartialGammaInv_xx, xPartialGammaInv_xy,
                                 xPartialGammaInv_xy, xPartialGammaInv_yy);
        }
        public static double2x2 comput_yPartialGammaInv_ij(double2 xy, double2 aM)
        {
            double2x2 gamma_ij = comput_Gamma_ij(xy, aM);
            double2x2 gammaInv_ij = comput_GammaInv_ij(gamma_ij);
            double2x2 yPartialGamma_ij = comput_yPartialGamma_ij(xy, aM);

            double gxx = gammaInv_ij[0][0];
            double gxy = gammaInv_ij[0][1];
            double gyy = gammaInv_ij[1][1];

            double dxx = yPartialGamma_ij[0][0];
            double dxy = yPartialGamma_ij[0][1];
            double dyy = yPartialGamma_ij[1][1];

            double yPartialGammaInv_xx = -(gxx * gxx * dxx + 2 * gxx * gxy * dxy + gxy * gxy * dyy);
            double yPartialGammaInv_xy = -(gxx * gxy * dxx + (gxx * gyy + gxy * gxy) * dxy + gxy * gyy * dyy);
            double yPartialGammaInv_yy = -(gxy * gxy * dxx + 2 * gxy * gyy * dxy + gyy * gyy * dyy);

            return new double2x2(yPartialGammaInv_xx, yPartialGammaInv_xy,
                                 yPartialGammaInv_xy, yPartialGammaInv_yy);
        }
        #endregion


        //----------------------------------------GR相关辅助方法---------------------------------------------

        #region GR相关辅助方法（简化形式）
        /// <summary>
        /// 从dx/dt转换成协变速度（注意是几何制）
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="beta_i"></param>
        /// <param name="gamma_ij"></param>
        /// <param name="dxdt"></param>
        public static double2 comput_U(double alpha, double2 beta_i, double2x2 gamma_ij, double2 dxdt)
        {
            double2 dxdt_rel_beta = dxdt + beta_i;
            double kinetic_term = math.dot(dxdt_rel_beta, math.mul(gamma_ij, dxdt_rel_beta));

            double u0 = 1.0 / math.sqrt(alpha * alpha - kinetic_term);

            double2 u = u0 * math.mul(gamma_ij, dxdt_rel_beta);
            return u;
        }
        /// <summary>
        /// 走时率的计算（几何制）
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="beta_i"></param>
        /// <param name="gamma_ij"></param>
        /// <param name="u"></param>
        /// <returns></returns>
        public static double comput_U0(double alpha, double2x2 gamma_ij, double2 u)
        {
            double2x2 gammaInv_ij = comput_GammaInv_ij(gamma_ij);
            return math.sqrt(math.dot(u, math.mul(gammaInv_ij, u)) + 1) / alpha;
        }
        #endregion

        #region GR相关辅助方法（独立形式）
        /// <summary>
        /// 从dx/dt转换成协变速度（注意是几何制）
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="beta_i"></param>
        /// <param name="gamma_ij"></param>
        /// <param name="dxdt"></param>
        public static double2 comput_U(double2 xy, double2 aM, double2 dxdt)
        {
            // 这里后续有时间了可以用辅助量简化计算
            double alpha = comput_Alpha(xy, aM);
            double2 beta_i = comput_Beta_i(xy, aM);
            double2x2 gamma_ij = comput_Gamma_ij(xy, aM);

            double2 dxdt_rel_beta = dxdt + beta_i;
            double kinetic_term = math.dot(dxdt_rel_beta, math.mul(gamma_ij, dxdt_rel_beta));

            double u0 = 1.0 / math.sqrt(alpha * alpha - kinetic_term);

            double2 u = u0 * math.mul(gamma_ij, dxdt_rel_beta);
            return u;
        }
        
        /// <summary>
        /// 走时率的计算（几何制）
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="beta_i"></param>
        /// <param name="gamma_ij"></param>
        /// <param name="u"></param>
        /// <returns></returns>
        public static double comput_U0(double2 xy, double2 aM, double2 u)
        {
            // 这里后续有时间了可以用辅助量简化计算
            double alpha = comput_Alpha(xy, aM);
            double2x2 gamma_ij = comput_Gamma_ij(xy, aM);

            double2x2 gammaInv_ij = comput_GammaInv_ij(gamma_ij);
            return math.sqrt(math.dot(u, math.mul(gammaInv_ij, u)) + 1) / alpha;
        }
        #endregion
    }
}