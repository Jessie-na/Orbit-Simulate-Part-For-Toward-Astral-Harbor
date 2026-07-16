using System;
using Unity.Mathematics;
using UnityEngine;

public class SimpleReactionWheel : MonoBehaviour
{
    [Header("动量轮设置")]
    public float torque;
    GRPhysicsObject currentGRPO;
    public void Start()
    {
        currentGRPO = GetComponentInParent<GRPhysicsObject>();
    }
    void FixedUpdate()
    {
        if (currentGRPO != Player.Instance.controlledObj || GRPhysicsEngine.Instance.timeWarp > 10)
        {
            currentGRPO.SetTorque(0);
            return;
        }
        float playerRequest = Player.Instance.TorqueRequest;
        if (playerRequest != 0) currentGRPO.torque = playerRequest * torque;
        // 简单的SAS程序
        else
        {
            if (currentGRPO.rotationSpeed > 0) currentGRPO.SetTorque(-torque);
            if (currentGRPO.rotationSpeed < 0) currentGRPO.SetTorque(torque);
        }
    }
}