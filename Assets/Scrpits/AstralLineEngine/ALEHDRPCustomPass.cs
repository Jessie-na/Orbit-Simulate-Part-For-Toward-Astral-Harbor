using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace AstralLineEngine
{
    class ALEHDRPCustomPass : CustomPass
    {
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        protected override void Execute(CustomPassContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample("ALE");
            ALEManager.Render(context.hdCamera.camera, context.cmd);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected override void Cleanup() { }
    }
}
