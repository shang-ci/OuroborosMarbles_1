using UnityEngine;

/// <summary>
/// 赋予被发射出去的珠子直线飞行的能力，并检测它与珠子链的碰撞。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class ShotMarble : MonoBehaviour
{
    [SerializeField]private float speed = 5f;

    void Awake() 
    { 
        GetComponent<Rigidbody2D>().gravityScale = 0; 
    }

    /// <summary>
    /// 发射珠子，设置其速度和方向
    /// </summary>
    /// <param name="direction"></param>
    public void Launch(Vector2 direction) 
    { 
        GetComponent<Rigidbody2D>().velocity = direction.normalized * speed; 
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("碰撞");
        Marble collidedMarble = collision.gameObject.GetComponent<Marble>();
        if (collidedMarble != null && collision.gameObject.GetComponent<ShotMarble>() == null)
        {
            int index = GameManager.Instance.GetMarbleIndex(collidedMarble);
            if (index != -1) GameManager.Instance.InsertMarble2(collidedMarble, GetComponent<Marble>());
            Destroy(gameObject);
        }
    }
}