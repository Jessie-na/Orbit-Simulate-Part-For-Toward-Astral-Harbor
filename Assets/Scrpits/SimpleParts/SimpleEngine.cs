using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// 一个简易的引擎系统，使飞船走非测地线运动
/// </summary>
public class SimpleEngine : MonoBehaviour
{
    [Header("引擎设置")]
    public float thrust = 0.1f;
    public float finalThrust; // 给UI调用输出引擎的实际推力
    public Transform debugArrow; // 显示引擎的出力
    GRPhysicsObject currentGRPO;
    void Start()
    {
        currentGRPO = GetComponentInParent<GRPhysicsObject>();
    }
    void FixedUpdate()
    {
        if (debugArrow) debugArrow.localScale = new(1, 1, Player.Instance.Throttle); // 将节流阀显示进arrow
        if (currentGRPO != Player.Instance.controlledObj || GRPhysicsEngine.Instance.timeWarp > 10)
        {
            finalThrust = 0;
            return;
        }
        // 计算指向（以z正轴为引擎指向）
        float3 localForward = new float3(0, 0, 1);
        float3 worldForward = math.rotate(transform.rotation, localForward);
        // 计算推力
        finalThrust = thrust * Player.Instance.Throttle;
        // 传进GRPO（一个GRPO只允许一个SimpleEngine，多引擎支持最好是和载具零件系统一起做）
        currentGRPO.SetForce(finalThrust * worldForward);
    }
}
