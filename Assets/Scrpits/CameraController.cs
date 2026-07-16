using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.CursorLockMode;

[ExecuteAlways]
[DefaultExecutionOrder(-350)]
public class CameraController : MonoBehaviour
{
    public Transform targetObject;
    public ViewMode currentViewMode;
    public float viewDistanceMin = 1;
    public float viewDistanceMax = 10000;
    public float cameraDamperRate = 17;
    [Tooltip("这个距离是近景相机与远景相机的分界线")]
    public float translateThreshold = 10000;
    public Transform baseRotor, targetRotor, damperRotor;
    public Camera nearCamera, farCamera; // 在编辑器里会麻烦一些
    Vector2 currViewPitchYaw;
    Vector2 dViewPitchYaw;
    float dViewScale = 1;
    bool rightButtonPressing;
    void Awake()
    {
        // 关闭射线检测
        nearCamera.eventMask = 0;
        farCamera.eventMask = 0;

        currentFP = GetComponent<HPFloatingPoint>();
    }
    void Start()
    {
        SetRotorRotation(targetRotor.rotation);
    }

    private HPFloatingPoint targetFP;
    private HPFloatingPoint currentFP;

    void FixedUpdate()
    {
        targetFP = targetObject.GetComponent<HPFloatingPoint>();
        currentFP.position = targetFP ? targetFP.position : (HPPos)targetObject.position;
    }

    void Update()
    {
        // 远近景相机自动切换
        bool isFar = targetRotor.localScale.z >= translateThreshold;
    }
    void LateUpdate()
    {
        targetFP = targetObject.GetComponent<HPFloatingPoint>();
        float damp = math.min(cameraDamperRate * Time.deltaTime, 1);

        //视角缩放
        float scale = targetRotor.localScale.z * dViewScale;
        targetRotor.localScale = (float3)Mathf.Clamp(scale, viewDistanceMin, viewDistanceMax);
        damperRotor.localScale = (float3)math.lerp(damperRotor.localScale.z, targetRotor.localScale.z, damp);

        bool freeView = Keyboard.current[Key.C].isPressed && Cursor.lockState is Locked;
        //目标转子运动
        if (rightButtonPressing || Cursor.lockState is Locked && !freeView)
            DoTargetRotate();
        //阻尼转子运动
        if (freeView)//自由视角
            damperRotor.localRotation *= Quaternion.Euler(dViewPitchYaw);
        else
            damperRotor.localRotation = math.slerp(damperRotor.localRotation, targetRotor.localRotation, damp);

        var effectiveMode = (currentViewMode == ViewMode.跟随)
            ? ViewMode.自由
            : currentViewMode;

        baseRotor.rotation = effectiveMode switch
        {
            ViewMode.跟随 => targetObject.rotation,
            ViewMode.默认 => Quaternion.identity,
            ViewMode.自由 => baseRotor.rotation,
            _ => Quaternion.identity,
        };
        currentFP.position = targetFP ? targetFP.position : (HPPos)targetObject.position;
    }


    #region 从PlayerInput调用的Actions
    public void OnRightBottonClick(InputAction.CallbackContext context)
    {
        rightButtonPressing = !context.canceled;
    }

    public void SetSlideDelta(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        dViewPitchYaw = new(-value.y, value.x);
    }

    public void SetZoomDelta(InputAction.CallbackContext context)
    {
        dViewScale = 1 - context.ReadValue<float>();
    }

    public void ChangeViewMode(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        int currIndex = (int)currentViewMode;
        currentViewMode = (ViewMode)(++currIndex % 3);//注意, 3是视角模式的数量
        //备份阻尼转子当前绝对旋转
        Quaternion currWorldRot = damperRotor.rotation;
        SetRotorRotation(targetRotor.rotation);
        //还原阻尼转子当前绝对旋转, 以防出现视角跳转
        damperRotor.rotation = currWorldRot;
        //MessageTip.ShowMessage($"当前视角模式:{currentViewMode}");
    }
    #endregion

    /// <summary> 设置当前相机转子的世界旋转, 以当前视角模式同步到本地储存的Pitch与Yaw. 旋转中相对于当前经纬参考系的滚转会自动清零 </summary>
    public void SetRotorRotation(Quaternion rotation, bool isLocalRot = false)
    {
        // 这里与 fixedUpdate 同样使用 effectiveMode 逻辑
        var effectiveMode = (currentViewMode == ViewMode.跟随)
            ? ViewMode.自由
            : currentViewMode;

        // 定位旋转基底
        baseRotor.SetPositionAndRotation(targetObject.position, effectiveMode switch
        {
            ViewMode.跟随 => targetObject.rotation,
            ViewMode.默认 => Quaternion.identity,
            ViewMode.自由 => baseRotor.rotation,
            _ => Quaternion.identity,
        });
        // 把Pitch与Yaw同步到本地
        Quaternion localRot = isLocalRot ? rotation : math.conjugate(baseRotor.rotation) * rotation;
        targetRotor.localRotation = localRot;
        currViewPitchYaw = new Vector2(MathF.IEEERemainder(localRot.eulerAngles.x, 180),
                                       MathF.IEEERemainder(localRot.eulerAngles.y, 360));
        // 应用本地Pitch与Yaw到转子的旋转
        DoTargetRotate();
    }

    public void SetRotorDir(Vector3 worldDir)
    {
        var rot = Quaternion.FromToRotation(Vector3.forward, worldDir);
        SetRotorRotation(rot);
    }

    void DoTargetRotate()
    {
        if (currentViewMode == ViewMode.自由)
        {
            targetRotor.Rotate(dViewPitchYaw);
        }
        else //目标转子本地经纬运动
        {
            currViewPitchYaw += dViewPitchYaw;
            currViewPitchYaw.x = Mathf.Clamp(currViewPitchYaw.x, -90, 90);
            currViewPitchYaw.y = MathF.IEEERemainder(currViewPitchYaw.y, 360);
            targetRotor.localEulerAngles = currViewPitchYaw;
        }
    }

}
public enum ViewMode // 如果要增删视角模式, 记得修改Update函数里的视角模式数量
{
    默认,
    自由,
    跟随
}

