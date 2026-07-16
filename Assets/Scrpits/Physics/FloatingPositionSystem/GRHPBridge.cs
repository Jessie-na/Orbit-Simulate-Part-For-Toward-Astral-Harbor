using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-400)]
public class GRHPBridge : MonoBehaviour
{
    GRPhysicsObject grpo;
    HPFloatingPoint currentHP;

    void Awake()
    {
        currentHP = GetComponent<HPFloatingPoint>();
        grpo = GetComponent<GRPhysicsObject>();
    }

    void Update()
    {
        double3 position = new(grpo.x.x, 0, grpo.x.y);
        currentHP.position = (HPPos)position;
        transform.eulerAngles = new(0, transform.eulerAngles.y, 0);
    }
}
