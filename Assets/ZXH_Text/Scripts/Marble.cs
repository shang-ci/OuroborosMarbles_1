// Marble.cs
// 职责：代表单个珠子，管理其外观（文字、滚动）和物理属性（直径）。

using UnityEngine;
using TMPro;

public class Marble : MonoBehaviour
{
    // [关键引用] 引用珠子上的 TextMeshPro 组件
    public TMP_Text characterText;

    // [核心数据] 记录此珠子在轨道上的精确路程距离。由 GameManager 更新。
    public float distanceOnPath;

    // [私有变量]
    private char _character;
    private SpriteRenderer _spriteRenderer;
    private float _currentRotationZ = 0f; // 累计旋转角度，以实现连续滚动
    private float _currentZRotation = 0f;

    // [公共属性] 向外部提供珠子的直径，用于精确的碰撞和间距计算
    public float Diameter => _spriteRenderer != null ? _spriteRenderer.bounds.size.x : 1f;

    void Awake()
    {
        // 自动获取 SpriteRenderer 组件，为计算直径做准备
        // 这里使用 GetComponentInChildren 是为了兼容把 Sprite 作为子对象的情况
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("Marble Prefab 必须包含一个 SpriteRenderer 组件！", this);
        }
    }

    /// <summary>
    /// 设置珠子显示的汉字。
    /// </summary>
    public void SetCharacter(char c)
    {
        _character = c;
        if (characterText != null)
        {
            characterText.text = c.ToString();
        }
    }

    /// <summary>
    /// 获取珠子代表的汉字。
    /// </summary>
    public char GetCharacter()
    {
        return _character;
    }

    ///// <summary>
    ///// [核心视觉逻辑 V2.0] 根据移动的方向和距离来更新珠子的滚动效果。
    ///// </summary>
    ///// <param name="movementVector">这一帧珠子移动的向量 (新位置 - 旧位置)</param>
    //public void UpdateRotation(Vector2 velocity)
    //{
    //    // 移动距离 = 速度大小 * 时间
    //    float distanceMoved = velocity.magnitude * Time.deltaTime;
    //    if (distanceMoved <= 0.0001f || Diameter <= 0) return;

    //    // 1. 计算旋转角度 (逻辑不变)
    //    float circumference = Mathf.PI * Diameter;
    //    float degreesToRotate = (distanceMoved / circumference) * 360f;

    //    // 2. 计算旋转轴 (逻辑不变)
    //    Vector3 rotationAxis = Vector3.Cross(velocity.normalized, Vector3.forward);

    //    // 3. 应用旋转 (逻辑不变)
    //    Quaternion deltaRotation = Quaternion.AngleAxis(degreesToRotate, rotationAxis);
    //    transform.rotation = deltaRotation * transform.rotation;
    //}

    /// <summary>
    /// 根据一帧内移动的距离，更新珠子的滚动效果（绕Z轴旋转）。
    /// </summary>
    /// <param name="distanceMoved">这一帧珠子移动的距离。</param>
    public void UpdateRotation(float distanceMoved)
    {
        if (distanceMoved <= 0.0001f || Diameter <= 0) return;
        float circumference = Mathf.PI * Diameter;
        float degreesToRotate = (distanceMoved / circumference) * 360f;
        _currentZRotation -= degreesToRotate;
        transform.rotation = Quaternion.Euler(0f, 0f, _currentZRotation);
    }
}