using UnityEngine;
using TMPro;

/// <summary>
/// 玩家的发射器，负责处理瞄准和发射动作。
/// </summary>
public class Launcher : MonoBehaviour
{
    public GameObject marblePrefab;
    public TMP_Text nextCharText;
    private char nextCharToShoot;

    void Start() 
    { 
        PrepareNextMarble(); 
    }

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90f是因为up轴为0度

        if (Input.GetMouseButtonDown(0))
        {
            Shoot(transform.up);
            PrepareNextMarble();
        }
    }

    /// <summary>
    /// 准备下一个要发射的珠子字符，并更新显示文本
    /// </summary>
    void PrepareNextMarble()
    {
        nextCharToShoot = GameManager.Instance.GetNextCharForLauncher();
        if (nextCharText != null) nextCharText.text = nextCharToShoot.ToString();
    }

    /// <summary>
    /// 发射珠子
    /// </summary>
    /// <param name="direction">发射速度</param>
    void Shoot(Vector2 direction)
    {
        GameObject shotMarbleObj = Instantiate(marblePrefab, transform.position, Quaternion.identity);
        shotMarbleObj.GetComponent<Marble>().SetCharacter(nextCharToShoot);
        shotMarbleObj.AddComponent<ShotMarble>().Launch(direction);
    }
}