using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Singleton<Player>
{
    [SerializeField] private float throttle = 0;
    public GameObject focusedObj; // 相机聚焦的对象
    public GRPhysicsObject controlledObj; // 玩家控制的对象
    // 公开节流阀接口
    public float Throttle
    {
        get
        {
            throttle = Mathf.Clamp(throttle, 0, 1);
            return throttle;
        }
    }
    // 力矩请求接口
    private float torqueRequest; 
    public float TorqueRequest
    {
        get => torqueRequest;
        set
        {
            if (value > 0) torqueRequest = 1;
            else if (value < 0) torqueRequest = -1;
            else torqueRequest = 0;
        }
    }

    float throttleIncreaseRate;

    void Update()
    {
        if (throttleIncreaseRate == 0) return;
        throttle += throttleIncreaseRate * Time.deltaTime;
        throttle = Mathf.Clamp(throttle, 0, 1);
    }

    #region 从PlayerInput调用的Actions
    // 控制滚转
    public void Torque(InputAction.CallbackContext context) =>
        torqueRequest = context.canceled ? 0 : (int)context.ReadValue<float>();
    // 控制节流阀
    public void OnIncreaseThrottle(InputAction.CallbackContext context) =>
        throttleIncreaseRate = context.canceled ? 0 : 0.6f;

    public void OnDecreaseThrottle(InputAction.CallbackContext context) =>
        throttleIncreaseRate = context.canceled ? 0 : -0.6f;

    public void MaxThrottle(InputAction.CallbackContext context) => throttle = 1;
    public void MinThrottle(InputAction.CallbackContext context) => throttle = 0;
    #endregion
}
