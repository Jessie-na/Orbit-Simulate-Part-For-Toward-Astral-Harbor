using UnityEngine;
using UnityEngine.UI;

public class PlanetUIMarker : MonoBehaviour
{
    [Header("外观设置")]
    [Tooltip("UI显示的图标贴图")]
    public Sprite iconSprite;
    [Tooltip("图标颜色")]
    public Color iconColor = Color.white;
    [Tooltip("图标在屏幕上的大小 (像素)")]
    public Vector2 iconSize = new Vector2(30, 30);

    [Header("依赖设置 (留空会自动寻找)")]
    public Canvas targetCanvas;
    private Camera mainCamera;

    // 内部生成的 UI 实例
    private GameObject iconObj;
    private RectTransform iconRect;
    private Image iconImage;

    void Start()
    {
        // 1. 获取相机和画布
        mainCamera = Camera.main;
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError($"[{gameObject.name}] 找不到 Canvas！请在场景中创建一个 Canvas。");
                return;
            }
        }

        // 2. 动态创建 UI Image
        iconObj = new GameObject($"{gameObject.name}_UIMarker");
        iconObj.transform.SetParent(targetCanvas.transform, false);

        iconRect = iconObj.AddComponent<RectTransform>();
        iconImage = iconObj.AddComponent<Image>();

        // 3. 应用自定义外观
        iconImage.sprite = iconSprite;
        iconImage.color = iconColor;
        iconRect.sizeDelta = iconSize;

        // 4. 设置锚点为左下角 (0,0)，这样 WorldToScreenPoint 的坐标才能完美对应
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.zero;
        iconRect.pivot = new Vector2(0.5f, 0.5f); // 中心点对齐
    }

    // 必须用 LateUpdate，确保在相机移动完毕后再更新 UI，防止图标延迟抖动
    void LateUpdate()
    {
        if (iconObj == null || mainCamera == null) return;

        // 获取星球在屏幕空间的坐标
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

        // screenPos.z > 0 表示物体在相机的前方
        // 如果物体跑到了相机背后，必须隐藏图标，否则它会在屏幕反面乱跑
        if (screenPos.z > 0)
        {
            if (!iconImage.enabled) iconImage.enabled = true;
            
            // 将 UI 移动到屏幕坐标位置
            iconRect.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            if (iconImage.enabled) iconImage.enabled = false;
        }
    }

    // 当星球被销毁或隐藏时，记得清理 UI 垃圾
    void OnDestroy()
    {
        if (iconObj != null)
        {
            Destroy(iconObj);
        }
    }

    void OnDisable()
    {
        if (iconObj != null) iconObj.SetActive(false);
    }

    void OnEnable()
    {
        if (iconObj != null) iconObj.SetActive(true);
    }
}