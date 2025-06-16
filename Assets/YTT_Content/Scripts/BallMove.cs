using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BallMove : MonoBehaviour
{
    public RectTransform startPoint; // 标题位置
    public RectTransform endPoint;   // 开始按钮位置
    public float duration;    // 单次动画时长
    public float pauseTime;     // 静止时间

    private RectTransform ballRect;

    void Start()
    {
        ballRect = GetComponent<RectTransform>();
        StartCoroutine(MoveLoop());
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            // 动画运动
            float timer = 0f;
            while (timer < duration)
            {
                float t = timer / duration;
                Vector2 controlPoint = (startPoint.anchoredPosition + endPoint.anchoredPosition) / 2 + Vector2.up * 100f;
                Vector2 pos = Mathf.Pow(1 - t, 2) * startPoint.anchoredPosition +
                              2 * (1 - t) * t * controlPoint +
                              Mathf.Pow(t, 2) * endPoint.anchoredPosition;
                ballRect.anchoredPosition = pos;
                timer += Time.deltaTime;
                yield return null;
            }
            // 保证最后停在终点
            ballRect.anchoredPosition = endPoint.anchoredPosition;
            // 静止2秒
            yield return new WaitForSeconds(pauseTime);
            // 回到起点
            ballRect.anchoredPosition = startPoint.anchoredPosition;
        }
    }
}