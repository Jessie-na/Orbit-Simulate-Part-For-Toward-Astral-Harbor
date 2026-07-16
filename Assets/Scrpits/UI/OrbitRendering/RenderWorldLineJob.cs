using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using AstralLineEngine;

[BurstCompile]
public struct WorldLineDrawJob : IJob
{
    [ReadOnly] public NativeArray<WorldLinePoint> TargetLine;
    [ReadOnly] public NativeArray<WorldLinePoint> ReferenceLine;

    public ALECommandBuilder builder;
    public double2 universeOrigin;
    public float viewScale;
    public Color startColor;
    public Color endColor;
    public float width; 
    public float4x4 vpMatrix;
    public float2 screenRes;
    public double areaThreshold;
    public bool isRelative;
    public bool enableRender;

    public void Execute()
    {
        int targetCount = TargetLine.Length;
        int refCount = ReferenceLine.Length;
        if (targetCount < 2 || !enableRender) return;

        builder.PushLineWidth(width);
        int renderLimit = isRelative ? math.min(targetCount, refCount) : targetCount;

        // 计算并记录第一个点在渲染空间的坐标
        double2 lastP = TargetLine[0].Pos;
        if (isRelative) lastP -= ReferenceLine[0].Pos;
        lastP -= universeOrigin; // 必须减去渲染原点，否则坐标是错的

        int lastIndex = 0;

        for (int i = 2; i < renderLimit; i++)
        {

            // 获取当前点并转换到同一个渲染空间
            double2 currP = TargetLine[i].Pos;
            if (isRelative) currP -= ReferenceLine[i].Pos;
            currP -= universeOrigin;

            // 获取中间值
            int middleIndex = (i + lastIndex) / 2;
            double2 middleP = TargetLine[middleIndex].Pos;
            if (isRelative) middleP -= ReferenceLine[middleIndex].Pos;
            middleP -= universeOrigin;

            // 计算当前三角形的屏幕空间面积
            float2 lastP_screen = ProjectToPixel(lastP);
            float2 middleP_screen = ProjectToPixel(middleP);
            float2 currP_screen = ProjectToPixel(currP);
            float area = 0.5f * math.abs(
                (currP_screen.x - lastP_screen.x) * (middleP_screen.y - lastP_screen.y) -
                (middleP_screen.x - lastP_screen.x) * (currP_screen.y - lastP_screen.y)
            );
            
            if (area > areaThreshold * areaThreshold || i == renderLimit - 1)
            {
                float2 rA = (float2)(lastP * viewScale);
                float2 rB = (float2)(middleP * viewScale);
                float2 rC = (float2)(currP * viewScale);

                builder.Line(
                    new float3(rA.x, 0, rA.y),
                    new float3(rB.x, 0, rB.y),
                    Color.Lerp(startColor, endColor, (float)middleIndex / (targetCount - 1))
                );
                builder.Line(
                    new float3(rB.x, 0, rB.y),
                    new float3(rC.x, 0, rC.y),
                    Color.Lerp(startColor, endColor, (float)i / (targetCount - 1))
                );

                // 更新上一次画出来的点和方向
                lastP = currP;
                lastIndex = i;
            }
        }
        builder.PopLineWidth();
    }
    
    // 将世界坐标投影到屏幕坐标
    private float2 ProjectToPixel(double2 pos)
    {
        float3 worldPos = new float3((float)pos.x, 0, (float)pos.y);

        float4 clipPos = math.mul(vpMatrix, new float4(worldPos, 1.0f));

        return (clipPos.xy / clipPos.w * 0.5f + 0.5f) * screenRes;
    }
}