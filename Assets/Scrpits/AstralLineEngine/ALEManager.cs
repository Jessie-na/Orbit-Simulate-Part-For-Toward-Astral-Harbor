using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace AstralLineEngine
{
    public class ALEManager : MonoBehaviour
    {
        private static ALEManager _instance;
        public static ALEManager Instance => _instance ?? Init();

        private Material lineMaterial;
        private static Dictionary<int, Camera[]> cameraConfigs = new Dictionary<int, Camera[]>();

        // 双缓冲，防止渲染时数据被释放
        private List<RenderTask> currentFrameTasks = new List<RenderTask>();
        private List<RenderTask> lastFrameTasks = new List<RenderTask>();

        private List<PendingTask> pendingTasks = new List<PendingTask>();
        private int idCounter = 0;

        struct RenderTask { public GraphicsBuffer gb; public int id; public int count; }
        struct PendingTask { public ALECommandBuilder builder; public JobHandle handle; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static ALEManager Init()
        {
            if (_instance != null) return _instance;
            GameObject go = new GameObject("ALE_AutoManager");
            _instance = go.AddComponent<ALEManager>();
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;

            Shader s = Shader.Find("ALE/ALE_Line");
            if (s == null) s = Resources.Load<Shader>("ALE_Line");
            if (s != null)
            {
                _instance.lineMaterial = new Material(s);
            }
            else
            {
                Debug.LogWarning("[ALE] Shader 'ALE/ALE_Line' not found. ALE will not render lines.");
            }
            return _instance;
        }

        void OnEnable()
        {
            // HDRP：通过全局 CustomPass 注入渲染（正确时机：BeforePostProcess）
            if (!gameObject.TryGetComponent<CustomPassVolume>(out var volume))
            {
                volume = gameObject.AddComponent<CustomPassVolume>();
                volume.isGlobal = true;
                volume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                volume.customPasses.Add(new ALEHDRPCustomPass());
            }
        }

        void OnDisable()
        {
            ClearAllResources();
        }

        // HDRP CustomPass 调用入口
        public static void Render(Camera cam, CommandBuffer cmd)
        {
            if (Instance.lineMaterial == null || Instance.currentFrameTasks.Count == 0) return;
            RenderInternal(cam, cmd);
        }

        // 核心渲染逻辑
        private static void RenderInternal(Camera cam, CommandBuffer cmd)
        {
            // HDRP 的 projectionMatrix 已包含平台适配，直接计算 VP
            // Shader 侧用 _ALE_FlipY 修正 CustomPass RenderTexture 的 Y 轴
            Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
            cmd.SetGlobalMatrix("_ALE_VP", vp);
            // Windows (Direct3D): graphicsUVStartsAtTop=true → flipY=-1
            // macOS (Metal)、其他: graphicsUVStartsAtTop=false → flipY=1
            cmd.SetGlobalFloat("_ALE_FlipY", SystemInfo.graphicsUVStartsAtTop ? -1.0f : 1.0f);

            foreach (var task in Instance.currentFrameTasks)
            {
                if (cameraConfigs.TryGetValue(task.id, out var targets) && targets != null)
                {
                    bool match = false;
                    foreach (var t in targets) if (t == cam) match = true;
                    if (!match) continue;
                }

                cmd.SetGlobalBuffer("_LineBuffer", task.gb);
                cmd.DrawProcedural(Matrix4x4.identity, Instance.lineMaterial, 0, MeshTopology.Lines, task.count * 2);
            }
        }

        public static ALECommandBuilder GetBuilder(bool inGame = true)
        {
            return new ALECommandBuilder
            {
                buffer = new NativeList<ALELineData>(1024, Allocator.Persistent),
                uniqueID = Instance.idCounter++,
            };
        }

        public static void SetCameras(int id, Camera[] cams) => cameraConfigs[id] = cams;
        public static Camera[] GetCameras(int id) => cameraConfigs.TryGetValue(id, out var c) ? c : null;

        public void SubmitAsync(ALECommandBuilder builder, JobHandle handle)
        {
            pendingTasks.Add(new PendingTask { builder = builder, handle = handle });
        }

        public void Submit(ALECommandBuilder builder)
        {
            if (builder.buffer.IsCreated && builder.buffer.Length > 0)
            {
                var gb = new GraphicsBuffer(GraphicsBuffer.Target.Structured, builder.buffer.Length, 44);
                gb.SetData(builder.buffer.AsArray());
                currentFrameTasks.Add(new RenderTask { gb = gb, id = builder.uniqueID, count = builder.buffer.Length });
            }
            if (builder.buffer.IsCreated) builder.buffer.Dispose();
        }

        void Update()
        {
            // 1. 清理"上上帧"的显存（GPU 已经画完）
            foreach (var t in lastFrameTasks) t.gb?.Release();
            lastFrameTasks.Clear();

            // 2. 把上一帧的任务移到待清理列表
            lastFrameTasks.AddRange(currentFrameTasks);
            currentFrameTasks.Clear();

            // 3. 处理挂起的 Job
            for (int i = pendingTasks.Count - 1; i >= 0; i--)
            {
                if (pendingTasks[i].handle.IsCompleted)
                {
                    pendingTasks[i].handle.Complete();
                    Submit(pendingTasks[i].builder);
                    pendingTasks.RemoveAt(i);
                }
            }
        }

        private void ClearAllResources()
        {
            foreach (var t in currentFrameTasks) t.gb?.Release();
            foreach (var t in lastFrameTasks) t.gb?.Release();
            currentFrameTasks.Clear();
            lastFrameTasks.Clear();
            foreach (var p in pendingTasks) if (p.builder.buffer.IsCreated) p.builder.buffer.Dispose();
            pendingTasks.Clear();
        }
    }
}
