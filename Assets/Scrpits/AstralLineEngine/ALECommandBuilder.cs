using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AstralLineEngine
{
    // 显存线段结构
    public struct ALELineData
    {
        public float3 start, end;
        public float4 color;
        public float width;
    }

    public struct ALECommandBuilder : IDisposable
    {
        public NativeList<ALELineData> buffer;
        internal int uniqueID;

        // 状态栈：使用固定大小数组以兼容 Burst
        private float currentWidth;
        private unsafe fixed float widthStack[8];
        private int stackPointer;

        public Camera[] cameraTargets
        {
            get => ALEManager.GetCameras(uniqueID);
            set => ALEManager.SetCameras(uniqueID, value);
        }

        public void PushLineWidth(float width)
        {
            unsafe
            {
                if (stackPointer < 8) widthStack[stackPointer++] = currentWidth;
            }
            currentWidth = width;
        }

        public void PopLineWidth()
        {
            unsafe
            {
                if (stackPointer > 0) currentWidth = widthStack[--stackPointer];
            }
        }

        public void Line(float3 a, float3 b, Color c)
        {
            buffer.Add(new ALELineData
            {
                start = a,
                end = b,
                color = (float4)(Vector4)c,
                width = currentWidth
            });
        }

        public void Dispose() => ALEManager.Instance.Submit(this);
        public void DisposeAfter(JobHandle handle) => ALEManager.Instance.SubmitAsync(this, handle);
    }
}