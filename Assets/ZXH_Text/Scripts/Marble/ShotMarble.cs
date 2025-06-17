using UnityEngine;

/// <summary>
/// ���豻�����ȥ������ֱ�߷��е��������������������������ײ��
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class ShotMarble : MonoBehaviour
{
    [SerializeField]private float speed = 7f;

    void Awake() 
    { 
        GetComponent<Rigidbody2D>().gravityScale = 0; 
    }

    /// <summary>
    /// �������ӣ��������ٶȺͷ���
    /// </summary>
    /// <param name="direction"></param>
    public void Launch(Vector2 direction) 
    { 
        GetComponent<Rigidbody2D>().velocity = direction.normalized * speed; 
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        AudioManager.Instance.PlaySFX("Marble"); // ������ײ��Ч   

        Marble collidedMarble = collision.gameObject.GetComponent<Marble>();
        if (collidedMarble != null && collision.gameObject.GetComponent<ShotMarble>() == null)
        {
            int index = GameManager.Instance.GetMarbleIndex(collidedMarble);
            if (index != -1) GameManager.Instance.InsertMarble2(collidedMarble, GetComponent<Marble>());
            Destroy(gameObject);
        }
    }
}