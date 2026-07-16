using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.Mathematics;
using UnityEngine.UI;
/// <summary>
/// UI管理类
/// </summary>
public class InfoUI : MonoBehaviour
{
    public TextMeshProUGUI velocityUI;
    public TextMeshProUGUI radialHeightUI;
    public TextMeshProUGUI properTimeUI;
    public TextMeshProUGUI coordinateTimeUI;
    public Slider throttleUI;
    readonly List<Action> updateFuncs = new();
    int currActionIndex;
    [Header("目标对象")]
    public GRPhysicsObject GRPO;
    GRWorldLineManager GRWLM;

    void Start()
    {
        GRWLM = GRPhysicsEngine.Instance.GetComponent<GRWorldLineManager>();
        updateFuncs.Add(UpdateVelocity);
        updateFuncs.Add(UpdateRadialHeight);
        updateFuncs.Add(UpdateProperTime);
        updateFuncs.Add(UpdateCoordinateTime);
        updateFuncs.Add(UpdateThrottle);
    }

    void Update()
    {
        if (!GRPO || !GRPhysicsEngine.Instance) return;
        updateFuncs[currActionIndex++].Invoke();
        currActionIndex %= updateFuncs.Count;
    }
    
    void UpdateVelocity()
    {
        float u = math.length((float2)GRPO.u);
        velocityUI.text = $"Velocity: {u:F3}";
    }
    void UpdateRadialHeight()
    {
        float2 refX = GRWLM.referenceCenter ? (float2)GRWLM.referenceCenter.x : float2.zero;
        float r = math.length((float2)GRPO.x - refX);
        radialHeightUI.text = $"Radial Height: {r:F3}";
    }
    void UpdateProperTime()
    {
        properTimeUI.text = $"Proper Time: {GRPO.properTime:F1}";
    }
    void UpdateCoordinateTime()
    {
        coordinateTimeUI.text = $"Coordinate Time: {GRPhysicsEngine.Instance.globalTime:F1}";
    }
    void UpdateThrottle()
    {
        if (!Player.Instance) return;
        throttleUI.value = Player.Instance.Throttle;
    }
}
