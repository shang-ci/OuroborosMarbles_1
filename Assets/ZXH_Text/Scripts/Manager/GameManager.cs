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

    [Header("关卡与游戏参数")]
    public int idiomsPerLevel = 10;      // 本关使用多少个成语
    public float marbleSpeed = 1f;       // 珠子链的基础移动速度
    public int initialMarbleCount = 20; // 这一关总共生成的珠子数量
    public float spawnAnimationSpeed = 0.1f; // 珠子生成的动画速度，值越小越快

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
    }

    void Start()
    {
        // [关键修改] 直接以轨道起点为基准生成
        // [关键修改] 使用协程来动态生成珠子，避免竞态条件并增加动画效果
        StartCoroutine(SpawnInitialChainCoroutine(initialMarbleCount));
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

        tempChain.Reverse();
        marbleChain = tempChain;
        RecalculateAllDistances();
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



    //void Update()
    //{
    //    // [核心循环] 每帧调用高效的移动方法
    //    MoveMarbleChainSmoothly();
    //}

    // Fix for CS1061: Replace the usage of `GetDistance` with a valid method or calculation.  
    // Based on the provided Spline type signatures, `GetCurveLength` can be used to calculate distances.  

    //private void MoveMarbleChainSmoothly()
    //{
    //    Debug.Log("MoveMarbleChainSmoothly called. Chain count: " + marbleChain.Count);

    //    if (marbleChain.Count == 0) return;

    //    // 1. 移动领导者 (链条的第一个珠子)  
    //    Marble headMarble = marbleChain[0];
    //    Vector3 oldPos = headMarble.transform.position;

    //    //使用我们新的辅助方法来获取距离
    //    float oldDistanceOnPath = GetDistanceOnSpline(headMarble.transform.position);  

    //    float newDistanceOnPath = oldDistanceOnPath + marbleSpeed * Time.deltaTime;

    //    // 更新领导者的位置和姿态 (使其垂直于轨道切线)  
    //    float newPosNormalized = pathSpline.Spline.ConvertIndexUnit(newDistanceOnPath, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //    headMarble.transform.position = pathSpline.EvaluatePosition(newPosNormalized);
    //    Vector3 newPos = headMarble.transform.position;
    //    Vector3 forward = (Vector3)pathSpline.EvaluateTangent(newPosNormalized);
    //    headMarble.transform.up = Vector3.Cross(forward, Vector3.forward);

    //    // 更新领导者的滚动效果  
    //    headMarble.UpdateRotation(newPos - oldPos);

    //    // 2. 移动所有跟随者，让它们紧跟前者  
    //    for (int i = 1; i < marbleChain.Count; i++)
    //    {
    //        Marble currentMarble = marbleChain[i];
    //        Marble marbleInFront = marbleChain[i - 1];

    //        // 目标：当前珠子的中心点与前一个珠子的中心点保持一个“直径和的一半”的距离  
    //        float targetSpacing = (marbleInFront.Diameter + currentMarble.Diameter) * 0.5f;
    //        float frontMarbleDistance = GetDistanceOnSpline(marbleInFront.transform.position);   
    //        float targetDistance = frontMarbleDistance - targetSpacing;

    //        //float currentDistance = GetDistanceOnSpline(headMarble.transform.position); 
    //        Vector3 oldFollowerPos = currentMarble.transform.position;

    //        // 直接设置到目标位置，也可以用 Lerp 做一个微小的平滑缓冲效果  
    //        float posNormalized = pathSpline.Spline.ConvertIndexUnit(targetDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //        currentMarble.transform.position = pathSpline.EvaluatePosition(posNormalized);
    //        Vector3 newFollowerPos = currentMarble.transform.position;
    //        forward = (Vector3)pathSpline.EvaluateTangent(posNormalized);
    //        currentMarble.transform.up = Vector3.Cross(forward, Vector3.forward);

    //        // 更新跟随者的滚动效果  
    //        currentMarble.UpdateRotation(newFollowerPos - oldFollowerPos);
    //    }
    //}

    private void MoveMarbleChainSmoothly()
    {
        if (marbleChain.Count == 0) return;

        // --- 1. 计算头车(Head Marble)的速度 ---
        Marble headMarble = marbleChain[0];
        Rigidbody2D headRb = headMarble.GetComponent<Rigidbody2D>();

        // 获取头车在轨道上的归一化位置 't'
        SplineUtility.GetNearestPoint(pathSpline.Spline, headMarble.transform.position, out _, out float t);

        // 获取该点的切线方向 (即前进方向)
        // 获取归一化的 float3 切线向量
        float3 tangentFloat3 = math.normalize(pathSpline.Spline.EvaluateTangent(t));
        // 手动创建 Vector2，只使用 x 和 y 分量
        Vector2 tangent = new Vector2(tangentFloat3.x, tangentFloat3.y);

        // 头车的速度就是基础速度乘以方向
        Vector2 headVelocity = tangent * marbleSpeed;
        headRb.velocity = headVelocity;

        // 根据速度更新旋转
        //headMarble.UpdateRotation(headVelocity);

        // --- 2. 计算所有跟随者(Follower Marbles)的速度 ---
        for (int i = 1; i < marbleChain.Count; i++)
        {
            Marble currentMarble = marbleChain[i];
            Marble marbleInFront = marbleChain[i - 1];
            Rigidbody2D currentRb = currentMarble.GetComponent<Rigidbody2D>();

            // 目标：让珠子间的距离保持为一个理想值 (半径之和)
            float targetSpacing = (marbleInFront.Diameter + currentMarble.Diameter) * 0.5f;
            float currentSpacing = Vector2.Distance(currentMarble.transform.position, marbleInFront.transform.position);

            // 计算距离误差
            float distanceError = currentSpacing - targetSpacing;

            // [核心算法：弹簧模型]
            // 根据距离误差来调整速度，形成一个“弹簧”效果
            // 如果距离太远 (error > 0)，就加速追赶
            // 如果距离太近 (error < 0)，就减速甚至后退来拉开距离
            // correctionFactor 决定了弹簧的“硬度”
            float correctionFactor = 5f;
            float speedAdjustment = distanceError * correctionFactor;

            // 跟随者的速度 = 基础速度 + 修正速度
            float followerSpeed = marbleSpeed + speedAdjustment;

            // 确保速度不会过快或变为负数（除非需要后退）
            followerSpeed = Mathf.Clamp(followerSpeed, 0, marbleSpeed * 2);

            // 获取跟随者当前位置的切线方向
            SplineUtility.GetNearestPoint(pathSpline.Spline, currentMarble.transform.position, out _, out float follower_t);
            float3 followerTangentFloat3 = math.normalize(pathSpline.Spline.EvaluateTangent(follower_t));
            Vector2 followerTangent = new Vector2(followerTangentFloat3.x, followerTangentFloat3.y);

            // 设置跟随者的最终速度
            Vector2 followerVelocity = followerTangent * followerSpeed;
            currentRb.velocity = followerVelocity;

            // 根据速度更新旋转
            //currentMarble.UpdateRotation(followerVelocity);
        }
    }

    // ... (其他方法，如 InsertMarble, CheckForMatches 等在下方) ...

    /// <summary>
    /// [平滑插入逻辑] 当发射的珠子碰撞后，在此处执行插入操作。
    /// </summary>
    public void InsertMarble(int collisionIndex, Marble shotMarble)
    {
        // 实例化新珠子，并设置它的文字
        GameObject newMarbleObj = Instantiate(marblePrefab, shotMarble.transform.position, Quaternion.identity);
        Marble newMarble = newMarbleObj.GetComponent<Marble>();
        newMarble.SetCharacter(shotMarble.GetCharacter());

        // 将新珠子插入到 List 中的正确位置
        int insertionIndex = collisionIndex + 1;
        marbleChain.Insert(insertionIndex, newMarble);

        // **不再调用全局更新！** 移动循环会在下一帧自动将后面的珠子平滑推开。

        // 插入后立即检查是否能组成成语
        CheckForMatches(insertionIndex);
    }

    /// <summary>
    /// [消除逻辑] 检查指定位置附近是否能组成成语。
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
    /// [彻底重构 V2.0] 以轨道起点为基准，向后生成指定数量的珠子。
    /// </summary>
    /// <param name="count">要生成的珠子数量</param>
    //private IEnumerator SpawnInitialChainCoroutine(int count)
    //{
    //    Debug.Log("Spawning coroutine started...");

    //    // 清空旧链条
    //    foreach (var marble in marbleChain) Destroy(marble.gameObject);
    //    marbleChain.Clear();

    //    for (int i = 0; i < count; i++)
    //    {
    //        // 1. 从数据管理器获取下一个字
    //        char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
    //        if (nextChar == '?') break;

    //        // 2. 实例化新珠子
    //        GameObject newMarbleObj = Instantiate(marblePrefab);
    //        Marble newMarble = newMarbleObj.GetComponent<Marble>();
    //        newMarble.SetCharacter(nextChar);

    //        // 3. 将新珠子放置在轨道起点 (distance 0)
    //        // 这是它的唯一职责！
    //        float posNormalized = pathSpline.Spline.ConvertIndexUnit(0, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //        newMarble.transform.position = pathSpline.EvaluatePosition(posNormalized);

    //        // 4. 将新珠子添加到链条的“头部”（索引0）
    //        marbleChain.Insert(0, newMarble);

    //        // 5. 等待一小段时间，以控制生成的速度
    //        yield return new WaitForSeconds(spawnAnimationSpeed);
    //    }
    //}

    //private IEnumerator SpawnInitialChainCoroutine(int count)
    //{
    //    foreach (var marble in marbleChain) Destroy(marble.gameObject);
    //    marbleChain.Clear();

    //    for (int i = 0; i < count; i++)
    //    {
    //        char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
    //        if (nextChar == '?') break;

    //        // 实例化珠子，并放置在轨道起点
    //        GameObject newMarbleObj = Instantiate(marblePrefab, pathSpline.Spline.EvaluatePosition(0), Quaternion.identity);
    //        Marble newMarble = newMarbleObj.GetComponent<Marble>();
    //        newMarble.SetCharacter(nextChar);

    //        // 将新珠子添加到链条的头部（索引0）
    //        // 这一步是关键，让它成为新的“头车”
    //        marbleChain.Insert(0, newMarble);

    //        // 等待一小段时间
    //        yield return new WaitForSeconds(spawnAnimationSpeed);
    //    }
    //}


    /// <summary>
    /// [公共API] 提供给 Launcher 获取下一个要发射的字。
    /// </summary>
    public char GetNextCharForLauncher()
    {
        return _idiomData.GetNextCharacterToShoot();
    }

    // 在 GameManager.cs 中添加这个公共方法
    public int GetMarbleIndex(Marble marble)
    {
        return marbleChain.IndexOf(marble);
    }

    // 在 GameManager.cs 脚本内部，任何其他方法之外添加

    /// <summary>
    /// [核心辅助方法] 计算一个世界坐标点在轨道上的最近点的路程距离。
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


}