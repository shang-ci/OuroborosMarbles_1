// GameManager.cs
// 职责：游戏流程的总控制中心。管理珠子链的生成、移动、插入和消除。

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using System.Collections;
using Unity.Mathematics; // 确保使用正确的 Spline 命名空间

public class GameManager : MonoBehaviour
{
    // [单例模式] 方便其他脚本（如Launcher）访问
    public static GameManager Instance;

    [Header("核心资源引用")]
    public TextAsset idiomFile;          // 拖入 chengyu.txt
    public GameObject marblePrefab;      // 拖入 Marble Prefab
    public SplineContainer pathSpline;   // 拖入场景中的轨道对象 (Path)
    public GameObject spawnPointObj;        // 拖入场景中的出生点对象 (SpawnPoint)

    [Header("关卡与游戏参数")]
    public int idiomsPerLevel = 10;      // 本关使用多少个成语
    public float marbleSpeed = 1f;       // 珠子链的基础移动速度
    public int initialMarbleCount = 20; // 这一关总共生成的珠子数量
    public float spawnAnimationSpeed = 0.1f; // 珠子生成的动画速度，值越小越快
    [SerializeField]private int currentchengyu = 0; // 消除的成语个数

    // [数据管理器] 引用我们设计的 IdiomDataManager
    private IdiomDataManager _idiomData;

    // [核心数据结构] 存储场景中所有珠子的动态列表
    [SerializeField]private List<Marble> marbleChain = new List<Marble>();

    void Awake()
    {
        // 设置单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // [关键初始化] 在游戏开始时，创建并初始化数据管理器
        _idiomData = new IdiomDataManager(idiomFile, idiomsPerLevel);

        AudioManager.Instance.PlayBGM("BattleSouce"); // 播放游戏开始音效
    }

    void Start()
    {
        //出生点
        if (spawnPointObj != null && pathSpline != null)
        {
            spawnPointObj.transform.position = pathSpline.Spline.EvaluatePosition(0f);
        }

        // [关键修改] 直接以轨道起点为基准生成
        // [关键修改] 使用协程来动态生成珠子，避免竞态条件并增加动画效果
        StartCoroutine(SpawnInitialChainCoroutine(initialMarbleCount));
    }

    private void Update()
    {
        if (currentchengyu == idiomsPerLevel)
        {
            GameWin();
        }

        float totalLength = pathSpline.Spline.GetLength();
        if (marbleChain.Count > 0 && marbleChain[0].distanceOnPath >= totalLength)
        {
            GameOver();
        }
    }


    /// <summary>
    /// [核心移动逻辑] 在固定的时间间隔执行，以实现平滑、物理精确的移动。
    /// </summary>
    void FixedUpdate()
    {
        if (marbleChain.Count == 0) return;

        // --- 步骤 1: 计算每个珠子的“理想路程距离” (Target Distance) ---
        List<float> targetDistances = CalculateTargetDistances();

        // --- 步骤 2: 根据“理想位置”驱动 Rigidbody2D 的速度 ---
        DriveMarblesToTargets(targetDistances);
    }

    /// <summary>
    /// 纯数学计算，得出本帧每个珠子应该在的精确路程距离。
    /// </summary>
    private List<float> CalculateTargetDistances()
    {
        List<float> distances = new List<float>(marbleChain.Count);
        if (marbleChain.Count == 0) return distances;

        // 头车的理想距离是当前距离加上一小段位移
        distances.Add(marbleChain[0].distanceOnPath + marbleSpeed * Time.fixedDeltaTime);

        // 计算所有跟随者的理想距离
        for (int i = 1; i < marbleChain.Count; i++)
        {
            float targetSpacing = (marbleChain[i - 1].Diameter + marbleChain[i].Diameter) * 0.5f;
            float followerTargetDistance = distances[i - 1] - targetSpacing;

            // 为防止抖动，只有当珠子被拉开时才更新它的目标位置
            distances.Add(Mathf.Max(followerTargetDistance, marbleChain[i].distanceOnPath));
        }
        return distances;
    }

    /// <summary>
    /// 为每个珠子赋予一个速度，使其平滑地朝自己的目标位置移动。
    /// </summary>
    private void DriveMarblesToTargets(List<float> targetDistances)
    {
        for (int i = 0; i < marbleChain.Count; i++)
        {
            Marble marble = marbleChain[i];
            marble.distanceOnPath = targetDistances[i]; // 更新珠子自己的距离记录

            float posNormalized = pathSpline.Spline.ConvertIndexUnit(marble.distanceOnPath, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            Vector3 targetPosition = pathSpline.EvaluatePosition(posNormalized);

            // 核心混合算法：速度 = (目标位置 - 当前位置) / 时间
            Vector3 movementVector = (targetPosition - marble.transform.position);
            marble.GetComponent<Rigidbody2D>().velocity = movementVector / Time.fixedDeltaTime;

            // 更新外观
            float distanceMoved = marble.GetComponent<Rigidbody2D>().velocity.magnitude * Time.fixedDeltaTime;
            marble.UpdateRotation(distanceMoved);

            float3 tangentFloat3 = pathSpline.Spline.EvaluateTangent(posNormalized);
            marble.transform.up = Vector3.Cross(new Vector2(tangentFloat3.x, tangentFloat3.y), Vector3.forward);
        }
    }

    /// <summary>
    /// [生成逻辑] 使用协程，在游戏开始时逐个创建珠子，形成动画效果。
    /// </summary>
    private IEnumerator SpawnInitialChainCoroutine(int count)
    {
        foreach (var m in marbleChain) if (m != null) Destroy(m.gameObject);
        marbleChain.Clear();

        float totalOffset = 0f;
        List<Marble> tempChain = new List<Marble>();

        for (int i = 0; i < count; i++)
        {
            char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
            if (nextChar == '?') break;

            GameObject newMarbleObj = Instantiate(marblePrefab, pathSpline.Spline.EvaluatePosition(0), Quaternion.identity);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();
            newMarble.SetCharacter(nextChar);

            float currentOffset = (i > 0) ? (tempChain[i - 1].Diameter + newMarble.Diameter) * 0.5f : newMarble.Diameter * 0.5f;
            totalOffset += currentOffset;
            newMarble.distanceOnPath = totalOffset;

            tempChain.Add(newMarble);
            yield return null;
        }

        //tempChain.Reverse();
        marbleChain = tempChain;
        RecalculateAllDistances();

        // 生成完毕后，进行一次全局检查，消除初始就存在的成语
        yield return new WaitForEndOfFrame(); // 等待一帧，确保所有位置都已更新
        CheckAndEliminateAllMatches();
    }



    /// <summary>
    /// 在珠子链反转后，重新计算所有珠子的路程距离，以确保顺序正确。
    /// </summary>
    private void RecalculateAllDistances()
    {
        if (marbleChain.Count == 0) return;
        float headDistance = marbleChain[0].distanceOnPath; // 以反转后的头车为基准

        for (int i = 1; i < marbleChain.Count; i++)
        {
            float spacing = (marbleChain[i - 1].Diameter + marbleChain[i].Diameter) * 0.5f;
            headDistance -= spacing;
            marbleChain[i].distanceOnPath = headDistance;
        }
    }

    // ... (其他方法，如 InsertMarble, CheckForMatches 等在下方) ...

    /// <summary>
    /// 插入发射的珠子到链条中，碰撞时根据X坐标判断插入前后，并自动检测消除成语。
    /// </summary>
    /// <param name="collisionMarble">被碰撞的链条珠子</param>
    /// <param name="shotMarble">发射的珠子（已实例化）</param>
    public void InsertMarble(Marble collisionMarble, Marble shotMarble)
    {
        // 获取碰撞珠子在链表中的索引
        int collisionIndex = marbleChain.IndexOf(collisionMarble);
        if (collisionIndex == -1) return; // 防御性检查

        GameObject newMarbleObj = Instantiate(marblePrefab);
        Marble newMarble = newMarbleObj.GetComponent<Marble>();
        newMarble.SetCharacter(shotMarble.GetCharacter());

        // 判断插入到前面还是后面
        int insertIndex = collisionIndex;
        if (newMarble.transform.position.x > collisionMarble.transform.position.x)
        {
            // 发射珠在右侧，插入到后面
            insertIndex = collisionIndex + 1;
        }
        // 否则插入到前面（insertIndex = collisionIndex）

        // 将发射珠子插入到链表
        marbleChain.Insert(insertIndex, newMarble);

        // 插入后立即检测并消除所有成语（支持连锁）
        CheckAndEliminateAllMatches();
    }


    /// <summary>
    /// 插入发射的珠子到链条中，判断插入到前还是后，并设置distanceOnPath
    /// </summary>
    /// <param name="collisionMarble">被碰撞的链条珠子</param>
    /// <param name="shotMarble">发射的珠子（已实例化）</param>
    public void InsertMarble2(Marble collisionMarble, Marble shotMarble)
    {
        int collisionIndex = marbleChain.IndexOf(collisionMarble);
        if (collisionIndex == -1) return;

        GameObject newMarbleObj = Instantiate(marblePrefab);
        Marble newMarble = newMarbleObj.GetComponent<Marble>();
        newMarble.SetCharacter(shotMarble.GetCharacter());

        int insertIndex = collisionIndex; // 默认插在前面
        float insertDistance = collisionMarble.distanceOnPath;

        // 判断前一个和后一个的距离
        float distToPrev = float.MaxValue;
        float distToNext = float.MaxValue;

        if (collisionIndex > 0)
        {
            var prevMarble = marbleChain[collisionIndex - 1];
            distToPrev = Vector2.Distance(shotMarble.transform.position, prevMarble.transform.position);
        }
        if (collisionIndex < marbleChain.Count - 1)
        {
            var nextMarble = marbleChain[collisionIndex + 1];
            distToNext = Vector2.Distance(shotMarble.transform.position, nextMarble.transform.position);
        }

        // 判断插入到前还是后
        if (distToPrev < distToNext)
        {
            // 插到前面
            insertIndex = collisionIndex;
            if (collisionIndex > 0)
            {
                // 线性插值distanceOnPath
                float prevDist = marbleChain[collisionIndex - 1].distanceOnPath;
                float currDist = collisionMarble.distanceOnPath;
                insertDistance = (prevDist + currDist) * 0.5f;
            }
            else
            {
                // 是第一个球，直接用当前球的distanceOnPath
                insertDistance = collisionMarble.distanceOnPath + shotMarble.Diameter;
            }
        }
        else
        {
            // 插到后面
            insertIndex = collisionIndex + 1;
            if (collisionIndex < marbleChain.Count - 1)
            {
                float nextDist = marbleChain[collisionIndex + 1].distanceOnPath;
                float currDist = collisionMarble.distanceOnPath;
                insertDistance = (nextDist + currDist) * 0.5f;
            }
            else
            {
                // 是最后一个球，直接用当前球的distanceOnPath
                insertDistance = collisionMarble.distanceOnPath - shotMarble.Diameter;
            }
        }

        // 设置发射珠子的distanceOnPath
        newMarble.distanceOnPath = insertDistance;

        // 插入到链表
        marbleChain.Insert(insertIndex, newMarble);

        // 立即消除成语
        CheckAndEliminateAllMatches();

        // 插入后立即检测并消除所有成语（支持连锁）
        CheckAndEliminateAllMatches();
    }

    /// <summary>
    /// 检查并消除所有可组成成语的珠子，支持连锁消除
    /// </summary>
    public void CheckAndEliminateAllMatches()
    {
        bool foundMatch;
        do
        {
            foundMatch = false;
            for (int i = 0; i <= marbleChain.Count - 4; i++)
            {
                string idiom = "";
                for (int j = 0; j < 4; j++)
                {
                    idiom += marbleChain[i + j].GetCharacter();
                }
                if (_idiomData.IsValidSessionIdiom(idiom))
                {
                    // 消除成语
                    for (int k = 0; k < 4; k++)
                    {
                        Destroy(marbleChain[i].gameObject); // 每次都移除i，因为后面的会前移
                        marbleChain.RemoveAt(i);
                    }
                    currentchengyu++; // 成语计数增加
                    foundMatch = true;
                    break; // 重新从头检测，支持连锁
                }
            }
        } while (foundMatch);
    }

    /// <summary>
    /// [消除逻辑] 检查指定位置附近是否能组成成语
    /// </summary>
    public void CheckForMatches(int insertionIndex)
    {
        // 检查以插入点为中心的所有4个可能的四字组合
        for (int i = 0; i < 4; i++)
        {
            int startIndex = insertionIndex - i;
            if (startIndex < 0 || startIndex + 3 >= marbleChain.Count) continue;

            string potentialIdiom = "";
            for (int j = 0; j < 4; j++)
            {
                potentialIdiom += marbleChain[startIndex + j].GetCharacter();
            }

            // [关键] 使用数据管理器来验证成语
            if (_idiomData.IsValidSessionIdiom(potentialIdiom))
            {
                Debug.Log("匹配成功: " + potentialIdiom);
                // 从后往前删除，避免索引错乱
                for (int k = 0; k < 4; k++)
                {
                    Destroy(marbleChain[startIndex + 3 - k].gameObject);
                    marbleChain.RemoveAt(startIndex + 3 - k);
                }

                // TODO: 在此可以添加连锁检查或吸附效果
                break; // 找到一个就先退出
            }
        }
    }

    /// <summary>
    /// [公共API] 提供给 Launcher 获取下一个要发射的字。
    /// </summary>
    public char GetNextCharForLauncher()
    {
        return _idiomData.GetNextCharacterToShoot();
    }

    /// <summary>
    /// 获取指定珠子在链中的索引位置
    /// </summary>
    /// <param name="marble">珠子</param>
    /// <returns></returns>
    public int GetMarbleIndex(Marble marble)
    {
        return marbleChain.IndexOf(marble);
    }


    /// <summary>
    /// 计算一个世界坐标点在轨道上的最近点的路程距离
    /// </summary>
    /// <param name="worldPosition">要查询的世界坐标</param>
    /// <returns>从轨道起点到该最近点的距离</returns>
    private float GetDistanceOnSpline(Vector3 worldPosition)
    {
        // 步骤1: 使用 SplineUtility.GetNearestPoint 找到离世界坐标最近的点。
        // 这个方法会输出很多信息，我们最需要的是 't'，它代表归一化的路径位置 (0 to 1)。
        SplineUtility.GetNearestPoint(pathSpline.Spline, worldPosition, out var nearestPoint, out float t);

        // 步骤2: 将归一化的位置 't' 转换成实际的路程距离。
        return pathSpline.Spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
    }


    #region 胜负
    /// <summary>
    /// 游戏胜利：所有珠子被消除或满足其它胜利条件时调用
    /// </summary>
    public void GameWin()
    {
        Debug.Log("游戏胜利！");
        // 停止所有珠子移动
        foreach (var marble in marbleChain)
        {
            var rb = marble.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        // 可扩展：播放胜利音效、弹窗、切换场景等
    }

    /// <summary>
    /// 游戏失败：如珠子链头到达终点时调用
    /// </summary>
    public void GameOver()
    {
        Debug.Log("游戏失败！");
        // 停止所有珠子移动
        foreach (var marble in marbleChain)
        {
            var rb = marble.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }
        // 可扩展：播放失败音效、弹窗、切换场景等

    }
    #endregion
}