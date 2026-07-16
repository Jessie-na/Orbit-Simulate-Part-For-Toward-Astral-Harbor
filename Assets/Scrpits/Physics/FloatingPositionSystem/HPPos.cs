using UnityEngine;
using System;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct HPPos
{
    // 划分网格的单位长度
    public const double SECTOR_SIZE = 1000000.0;

    // 所在格子的索引
    public long sectorX;
    public long sectorY;
    public long sectorZ;

    // 格子内的局部偏移
    public double localX;
    public double localY;
    public double localZ;


    // 构造函数
    public HPPos(long sx, long sy, long sz, double lx, double ly, double lz)
    {
        sectorX = sx; sectorY = sy; sectorZ = sz;
        localX = lx; localY = ly; localZ = lz;
        Normalize(); // 自动规整
    }
#region 构造函数
    public HPPos(Vector3 unityPosition)
    {
        sectorX = 0; sectorY = 0; sectorZ = 0;
        localX = unityPosition.x;
        localY = unityPosition.y;
        localZ = unityPosition.z;
        Normalize();
    }
    public HPPos(float x, float y, float z)
    {
        sectorX = 0; sectorY = 0; sectorZ = 0;
        localX = x; localY = y; localZ = z;
        Normalize();
    }
    public HPPos(double x,double y,double z)
    {
        sectorX = 0; sectorY = 0; sectorZ = 0;
        localX = x; localY = y; localZ = z;
        Normalize();
    }
    #endregion
    /// <summary>
    /// 检查 Local 坐标是否超出了格子范围，如果超出，则移动 Sector 索引。
    /// </summary>
    public void Normalize()
    {
        double halfSector = SECTOR_SIZE * 0.5;

        // X 轴处理
        if (localX >= halfSector || localX < -halfSector)
        {
            long shift = (long)Math.Round(localX / SECTOR_SIZE, MidpointRounding.AwayFromZero);
            sectorX += shift;
            localX -= shift * SECTOR_SIZE;
        }

        // Y 轴处理
        if (localY >= halfSector || localY < -halfSector)
        {
            long shift = (long)Math.Round(localY / SECTOR_SIZE, MidpointRounding.AwayFromZero);
            sectorY += shift;
            localY -= shift * SECTOR_SIZE;
        }

        // Z 轴处理
        if (localZ >= halfSector || localZ < -halfSector)
        {
            long shift = (long)Math.Round(localZ / SECTOR_SIZE, MidpointRounding.AwayFromZero);
            sectorZ += shift;
            localZ -= shift * SECTOR_SIZE;
        }
    }

    public double GetX() => localX + sectorX * SECTOR_SIZE;
    public double GetY() => localY + sectorY * SECTOR_SIZE;
    public double GetZ() => localZ + sectorZ * SECTOR_SIZE;

    // 核心运算

    public static HPPos operator +(HPPos a, HPPos b)
    {
        return new HPPos(
            a.sectorX + b.sectorX,
            a.sectorY + b.sectorY,
            a.sectorZ + b.sectorZ,
            a.localX + b.localX,
            a.localY + b.localY,
            a.localZ + b.localZ
        );
    }

    #region 不同类型之间的加法运算
    public static HPPos operator +(HPPos a, Vector3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX + b.x, a.localY + b.y, a.localZ + b.z);
    }

    public static HPPos operator +(HPPos a, double3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX + b.x, a.localY + b.y, a.localZ + b.z);
    }

    public static HPPos operator +(HPPos a, float3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX + b.x, a.localY + b.y, a.localZ + b.z);
    }

    public static HPPos operator +(Vector3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX + a.x, b.localY + a.y, b.localZ + a.z);
    }

    public static HPPos operator +(double3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX + a.x, b.localY + a.y, b.localZ + a.z);
    }

    public static HPPos operator +(float3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX + a.x, b.localY + a.y, b.localZ + a.z);
    }
    #endregion

    public static HPPos operator -(HPPos a, HPPos b)
    {
        return new HPPos(
            a.sectorX - b.sectorX,
            a.sectorY - b.sectorY,
            a.sectorZ - b.sectorZ,
            a.localX - b.localX,
            a.localY - b.localY,
            a.localZ - b.localZ
        );
    }

    #region 不同类型之间的减法运算
    public static HPPos operator -(HPPos a, double3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX - b.x, a.localY - b.y, a.localZ - b.z);
    }

    public static HPPos operator -(HPPos a, float3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX - b.x, a.localY - b.y, a.localZ - b.z);
    }

    public static HPPos operator -(HPPos a, Vector3 b)
    {
        return new HPPos(a.sectorX, a.sectorY, a.sectorZ, a.localX - b.x, a.localY - b.y, a.localZ - b.z);
    }

    public static HPPos operator -(Vector3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX - a.x, b.localY - a.y, b.localZ - a.z);
    }

    public static HPPos operator -(double3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX - a.x, b.localY - a.y, b.localZ - a.z);
    }

    public static HPPos operator -(float3 a, HPPos b)
    {
        return new HPPos(b.sectorX, b.sectorY, b.sectorZ, b.localX - a.x, b.localY - a.y, b.localZ - a.z);
    }
    #endregion

    public static double Dot(HPPos a, HPPos b)
    {
        double3 doubleA = (double3)a;
        double3 doubleB = (double3)b;
        return math.dot(doubleA, doubleB);
    }

    /// <summary>
    /// 高精度叉乘
    /// 采用分级拆分法：
    /// SS项 (Sector*Sector) =>  K² 级，直接进入 Sector
    /// Mix项 (Sector*Local) =>  K 级，拆分为整数部分(进Sector)和小数部分(进Local)
    /// LL项 (Local*Local)   =>  1 级，直接进入 Local
    /// </summary>
    public static HPPos Cross(HPPos a, HPPos b)
    {
        // X 分量: Ya * Zb - Za * Yb

        // 纯整数部分
        long x_SS = a.sectorY * b.sectorZ - a.sectorZ * b.sectorY;

        // 混合部分
        double x_Mix = (a.sectorY * b.localZ + a.localY * b.sectorZ)
                     - (a.sectorZ * b.localY + a.localZ * b.sectorY);

        // 拆分 Mix
        long x_Mix_Int = (long)System.Math.Floor(x_Mix);
        double x_Mix_Frac = x_Mix - x_Mix_Int;

        // 纯小数部分
        double x_LL = a.localY * b.localZ - a.localZ * b.localY;

        // 将Mix的整数部分和小数部分分别加到SS和LL中
        long finalSectorX = x_SS * (long)SECTOR_SIZE + x_Mix_Int;
        double finalLocalX = x_Mix_Frac * SECTOR_SIZE + x_LL;


        // Y 分量: Za * Xb - Xa * Zb

        long y_SS = a.sectorZ * b.sectorX - a.sectorX * b.sectorZ;

        double y_Mix = (a.sectorZ * b.localX + a.localZ * b.sectorX)
                     - (a.sectorX * b.localZ + a.localX * b.sectorZ);

        long y_Mix_Int = (long)System.Math.Floor(y_Mix);
        double y_Mix_Frac = y_Mix - y_Mix_Int;

        double y_LL = a.localZ * b.localX - a.localX * b.localZ;

        long finalSectorY = y_SS * (long)SECTOR_SIZE + y_Mix_Int;
        double finalLocalY = y_Mix_Frac * SECTOR_SIZE + y_LL;


        // Z 分量: Xa * Yb - Ya * Xb

        long z_SS = a.sectorX * b.sectorY - a.sectorY * b.sectorX;

        double z_Mix = (a.sectorX * b.localY + a.localX * b.sectorY)
                     - (a.sectorY * b.localX + a.localY * b.sectorX);

        long z_Mix_Int = (long)System.Math.Floor(z_Mix);
        double z_Mix_Frac = z_Mix - z_Mix_Int;

        double z_LL = a.localX * b.localY - a.localY * b.localX;

        long finalSectorZ = z_SS * (long)SECTOR_SIZE + z_Mix_Int;
        double finalLocalZ = z_Mix_Frac * SECTOR_SIZE + z_LL;

        // 我草，我真是太厉害了居然把这个叉乘运算做到分离超大数值计算把浮点误差控制到最低可把我牛逼坏了插一会儿腰 =^> w <^=
        return new HPPos(finalSectorX, finalSectorY, finalSectorZ, finalLocalX, finalLocalY, finalLocalZ);
    }

    public double sqrMagnitude
    {
        get
        {
            double absX = sectorX * SECTOR_SIZE + localX;
            double absY = sectorY * SECTOR_SIZE + localY;
            double absZ = sectorZ * SECTOR_SIZE + localZ;

            return absX * absX + absY * absY + absZ * absZ;
        }
    }


    // 辅助功能    
    #region 不同类型之间的转换
    public Vector3 ToVector3()
    {
        return new Vector3(
            (float)(sectorX * SECTOR_SIZE + localX),
            (float)(sectorY * SECTOR_SIZE + localY),
            (float)(sectorZ * SECTOR_SIZE + localZ)
        );
    }

    public double3 ToDouble3()
    {
        return new double3(
            sectorX * SECTOR_SIZE + localX,
            sectorY * SECTOR_SIZE + localY,
            sectorZ * SECTOR_SIZE + localZ
        );
    }

    public float3 ToFloat3()
    {
        return new float3(
            (float)(sectorX * SECTOR_SIZE + localX),
            (float)(sectorY * SECTOR_SIZE + localY),
            (float)(sectorZ * SECTOR_SIZE + localZ)
        );
    }

    public static explicit operator Vector3(HPPos pos)
    {
        return pos.ToVector3();
    }

    public static explicit operator double3(HPPos pos)
    {
        return pos.ToDouble3();
    }

    public static explicit operator float3(HPPos pos)
    {
        return pos.ToFloat3();
    }

    public HPPos(double3 xyz)
    {
        sectorX = 0; sectorY = 0; sectorZ = 0;
        localX = xyz.x; localY = xyz.y; localZ = xyz.z;
        Normalize(); // 把大数值切分进 Sector
    }

    public HPPos(float3 xyz)
    {
        sectorX = 0; sectorY = 0; sectorZ = 0;
        localX = xyz.x; localY = xyz.y; localZ = xyz.z;
        Normalize();
    }

    public static explicit operator HPPos(double3 v)
    {
        return new HPPos(v);
    }

    public static explicit operator HPPos(float3 v)
    {
        return new HPPos(v);
    }

    public static explicit operator HPPos(Vector3 v)
    {
        return new HPPos((float3)v);
    }
    #endregion

    public Vector3 IdentifyPositionFromOrigin(HPPos origin)
    {
        double dx = (this.sectorX - origin.sectorX) * SECTOR_SIZE + (this.localX - origin.localX);
        double dy = (this.sectorY - origin.sectorY) * SECTOR_SIZE + (this.localY - origin.localY);
        double dz = (this.sectorZ - origin.sectorZ) * SECTOR_SIZE + (this.localZ - origin.localZ);

        return new Vector3((float)dx, (float)dy, (float)dz);
    }
    
    public static double Distance(HPPos a, HPPos b)
    {
        double dx = (a.sectorX - b.sectorX) * SECTOR_SIZE + (a.localX - b.localX);
        double dy = (a.sectorY - b.sectorY) * SECTOR_SIZE + (a.localY - b.localY);
        double dz = (a.sectorZ - b.sectorZ) * SECTOR_SIZE + (a.localZ - b.localZ);
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public override string ToString()
    {
        return $"[S:({sectorX},{sectorY},{sectorZ}) L:({localX:F2},{localY:F2},{localZ:F2})]";
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HPPos))]
public class HPPosDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty p, GUIContent l) => 60f;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 主标题
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, 18), label, EditorStyles.boldLabel);
        
        float startX = position.x + 15f; 
        float w = position.width - 15f;
        
        DrawRow(new Rect(startX, position.y + 20, w, 18), property, "Sector", "sectorX", "sectorY", "sectorZ");
        DrawRow(new Rect(startX, position.y + 40, w, 18), property, "Local", "localX", "localY", "localZ");
        
        EditorGUI.EndProperty();
    }

    void DrawRow(Rect r, SerializedProperty p, string rowLabel, string x, string y, string z)
    {
        float rowLabelWidth = 50f; 
        float fieldWidth = (r.width - rowLabelWidth) / 3f;
        
        // 绘制行名 (Sector / Local)
        EditorGUI.LabelField(new Rect(r.x, r.y, rowLabelWidth, r.height), rowLabel, EditorStyles.miniLabel);
        
        // 修改原生标签宽度，让 X/Y/Z 不占据太多空间
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 12f; 

        // 计算三个框的位置
        Rect rx = new Rect(r.x + rowLabelWidth, r.y, fieldWidth - 2f, r.height);
        Rect ry = new Rect(r.x + rowLabelWidth + fieldWidth, r.y, fieldWidth - 2f, r.height);
        Rect rz = new Rect(r.x + rowLabelWidth + fieldWidth * 2, r.y, fieldWidth - 2f, r.height);

        EditorGUI.PropertyField(rx, p.FindPropertyRelative(x), new GUIContent("X"));
        EditorGUI.PropertyField(ry, p.FindPropertyRelative(y), new GUIContent("Y"));
        EditorGUI.PropertyField(rz, p.FindPropertyRelative(z), new GUIContent("Z"));

        // 恢复原生宽度设置，防止影响面板里的其他组件
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}
#endif