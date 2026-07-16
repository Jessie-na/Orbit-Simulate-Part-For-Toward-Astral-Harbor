using UnityEngine;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(GRPhysicsObject))]
public class GRStateSettingTools : MonoBehaviour
{
    public GRPhysicsObject currentObj => GetComponent<GRPhysicsObject>();
    public enum CoordinateMode { Cartesian, Polar }

    [Header("模式设置")]
    public CoordinateMode coordMode = CoordinateMode.Cartesian;
    public GRPhysicsObject referenceTarget;

    [Header("输入数据 (相对于参考系)")]
    public double2 cartesianPos;
    public double2 cartesianVel;
    public double radius;
    public double angleDeg;
    public double radialVel;
    public double tangentialVel;

    // --- 关键修正：将状态缓存序列化，防止 Play Mode 切换时重置 ---
    [SerializeField, HideInInspector] private GRPhysicsObject _lastReferenceTarget;
    [SerializeField, HideInInspector] private double2 _lastValidAbsX;
    [SerializeField, HideInInspector] private double2 _lastValidAbsU;

    private bool _isProcessing = false;

    private void OnEnable()
    {
        // 初始加载时对齐缓存，如果缓存是空的则抓取物理脚本当前值
        if (currentObj != null && math.all(_lastValidAbsX == 0))
        {
            _lastValidAbsX = currentObj.x;
            _lastValidAbsU = currentObj.u;
            _lastReferenceTarget = referenceTarget;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    private void OnValidate()
    {
        if (Application.isPlaying || _isProcessing || currentObj == null) return;
        _isProcessing = true;

#if UNITY_EDITOR
        // 1. 检测参考系切换逻辑
        // 如果 _lastReferenceTarget 为 null 且当前不为 null，说明是首次赋予参考系或从 PlayMode 恢复
        if (referenceTarget != _lastReferenceTarget)
        {
            // 只有当不是第一次加载，且绝对坐标有效时，才执行“保持位置不动”的逆向转换
            if (_lastReferenceTarget != null || !math.all(_lastValidAbsX == 0))
            {
                Undo.RecordObject(this, "Change Reference Frame");

                double2 newRefX = (referenceTarget != null) ? referenceTarget.x : double2.zero;
                double2 newRefU = (referenceTarget != null) ? referenceTarget.u : double2.zero;

                // 核心：基于切换前的绝对坐标反算相对坐标
                cartesianPos = _lastValidAbsX - newRefX;
                cartesianVel = _lastValidAbsU - newRefU;

                SyncPolarFromCartesian();
            }
            _lastReferenceTarget = referenceTarget;
        }
        else
        {
            // 2. 正常的数值修改逻辑
            Undo.RecordObject(this, "GR Tools Modify");

            if (coordMode == CoordinateMode.Cartesian)
                SyncPolarFromCartesian();
            else
                SyncCartesianFromPolar();
        }

        // 3. 统一更新并刷新缓存
        UpdatePhysicsAndTransform();
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(currentObj);
#endif
        _isProcessing = false;
    }

    private void EditorUpdate()
    {
        if (Application.isPlaying || _isProcessing) return;

#if UNITY_EDITOR
        // 只有选中物体时才处理同步，防止后台干扰
        if (Selection.activeGameObject == gameObject)
        {
            UpdatePhysicsAndTransform();
        }
#endif
    }

    private void UpdatePhysicsAndTransform()
    {
        if (Application.isPlaying) return; // 防止其在运行游戏时污染数据
        double2 refX = (referenceTarget != null) ? referenceTarget.x : double2.zero;
        double2 refU = (referenceTarget != null) ? referenceTarget.u : double2.zero;

        // 更新物理脚本
        currentObj.x = cartesianPos + refX;
        currentObj.u = cartesianVel + refU;

        // 重要：更新这一刻的绝对值缓存，确保下次 OnValidate 知道“起点”在哪里
        _lastValidAbsX = currentObj.x;
        _lastValidAbsU = currentObj.u;

        // 同步场景渲染位置
        if (TryGetComponent<HPFloatingPoint>(out HPFloatingPoint currentFP))
            currentFP.position = new HPPos(currentObj.x.x, 0, currentObj.x.y);
        else
            transform.position = new float3((float)currentObj.x.x, 0, (float)currentObj.x.y);
    }

    private void SyncPolarFromCartesian()
    {
        radius = math.length(cartesianPos);
        angleDeg = math.degrees(math.atan2(cartesianPos.y, cartesianPos.x));
        if (radius > 1e-6)
        {
            double2 dir = cartesianPos / radius;
            double2 tangent = new double2(-dir.y, dir.x);
            radialVel = math.dot(cartesianVel, dir);
            tangentialVel = math.dot(cartesianVel, tangent);
        }
    }

    private void SyncCartesianFromPolar()
    {
        double rad = math.radians(angleDeg);
        double cos = math.cos(rad);
        double sin = math.sin(rad);
        cartesianPos = new double2(radius * cos, radius * sin);
        cartesianVel = new double2(radialVel * cos - tangentialVel * sin,
                                   radialVel * sin + tangentialVel * cos);
    }
}

[CustomEditor(typeof(GRStateSettingTools))]
[CanEditMultipleObjects]
public class GRStateSettingToolsEditor : Editor
{
    // 定义序列化属性
    SerializedProperty coordModeProp;
    SerializedProperty referenceTargetProp;
    
    SerializedProperty cartesianPosProp;
    SerializedProperty cartesianVelProp;
    
    SerializedProperty radiusProp;
    SerializedProperty angleDegProp;
    SerializedProperty radialVelProp;
    SerializedProperty tangentialVelProp;

    private void OnEnable()
    {
        // 链接属性
        coordModeProp = serializedObject.FindProperty("coordMode");
        referenceTargetProp = serializedObject.FindProperty("referenceTarget");
        
        cartesianPosProp = serializedObject.FindProperty("cartesianPos");
        cartesianVelProp = serializedObject.FindProperty("cartesianVel");
        
        radiusProp = serializedObject.FindProperty("radius");
        angleDegProp = serializedObject.FindProperty("angleDeg");
        radialVelProp = serializedObject.FindProperty("radialVel");
        tangentialVelProp = serializedObject.FindProperty("tangentialVel");
    }

    public override void OnInspectorGUI()
    {
        GRStateSettingTools tools = (GRStateSettingTools)target;

        // 更新序列化对象状态
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("核心设置", EditorStyles.boldLabel);
        
        // 绘制通用设置
        EditorGUILayout.PropertyField(coordModeProp, new GUIContent("坐标系模式"));
        EditorGUILayout.PropertyField(referenceTargetProp, new GUIContent("参考天体 (Target)"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("数值配置 (相对于参考系)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 根据模式隐藏/显示变量
        if (tools.coordMode == GRStateSettingTools.CoordinateMode.Cartesian)
        {
            // 笛卡尔模式
            DrawDouble2Property(cartesianPosProp, "局部位置 (x, y)");
            DrawDouble2Property(cartesianVelProp, "局部速度 (ux, uy)");
        }
        else
        {
            // 极坐标模式
            DrawDraggableDouble(radiusProp, "轨道半径 (Radius)");
            DrawDraggableDouble(angleDegProp, "极角 (Angle Deg)");
            
            EditorGUILayout.Space(5);
            
            DrawDraggableDouble(radialVelProp, "径向速度 (Vr)");
            DrawDraggableDouble(tangentialVelProp, "切向速度 (Vt)");
        }

        EditorGUILayout.EndVertical();

        // 应用修改
        if (serializedObject.ApplyModifiedProperties())
        {
            // 如果属性改变，在非运行模式下触发同步逻辑
            if (!Application.isPlaying)
            {
                // 这里利用了 SerializedProperty 的修改会自动触发脚本的 OnValidate
            }
        }
        
        // 强制重绘 Scene 视图，让物体跟随鼠标拖动实时移动
        if (GUI.changed && !Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }

    /// <summary>
    /// 绘制支持鼠标标签拖拽的 Double 字段
    /// </summary>
    private void DrawDraggableDouble(SerializedProperty prop, string label)
    {
        // 使用 EditorGUILayout.PropertyField 会自动生成支持拖拽的标签（Unity 2020+ 支持更好）
        // 如果你需要更细致的拖拽感，可以在这里自定义逻辑
        EditorGUILayout.PropertyField(prop, new GUIContent(label));
    }

    /// <summary>
    /// 专门为 double2 类型设计的绘制逻辑（x 和 y 水平排列且可拖拽）
    /// </summary>
    private void DrawDouble2Property(SerializedProperty prop, string label)
    {
        SerializedProperty xProp = prop.FindPropertyRelative("x");
        SerializedProperty yProp = prop.FindPropertyRelative("y");

        Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        EditorGUI.PrefixLabel(rect, new GUIContent(label));

        float halfWidth = (rect.width - EditorGUIUtility.labelWidth) / 2f;
        
        Rect xRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, halfWidth - 2, rect.height);
        Rect yRect = new Rect(rect.x + EditorGUIUtility.labelWidth + halfWidth, rect.y, halfWidth, rect.height);

        // 绘制 X 和 Y，通过 Label 缩写标识
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 15f; 
        
        EditorGUI.PropertyField(xRect, xProp, new GUIContent("X"));
        EditorGUI.PropertyField(yRect, yProp, new GUIContent("Y"));
        
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}