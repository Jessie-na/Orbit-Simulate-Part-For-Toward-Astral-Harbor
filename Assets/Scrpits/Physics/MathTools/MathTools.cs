namespace Unity.Mathematics
{
    public class MathTools
    {
        /// <summary>
        /// 双精度矢量旋转方法
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double3 Rotate(quaternion rotation, double3 vector)
        {
            // 将 float 四元数转为 double 向量分量
            double4 q = new double4(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);

            double3 q_xyz = q.xyz;
            // 使用 double3 的叉乘
            double3 t = 2.0 * math.cross(q_xyz, vector);
            // 公式：v + w*t + cross(xyz, t)
            return vector + q.w * t + math.cross(q_xyz, t);
        }
        public static double2 Rotate(quaternion rotation, double2 vector)
        {
            double3 vector3 = new(vector.x, 0, vector.y);
            // 将 float 四元数转为 double 向量分量
            double4 q = new double4(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);

            double3 q_xyz = q.xyz;
            // 使用 double3 的叉乘
            double3 t = 2.0 * math.cross(q_xyz, vector3);
            // 公式：v + w*t + cross(xyz, t)
            double3 result3 = vector3 + q.w * t + math.cross(q_xyz, t);
            return new(result3.x, result3.z);
        }
    }
}