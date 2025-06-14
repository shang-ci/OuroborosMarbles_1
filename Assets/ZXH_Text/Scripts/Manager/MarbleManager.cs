using UnityEngine;
using System.Collections.Generic; // 使用 List
using System.IO;                  // 用于读取文件
using UnityEngine.Splines;
using System.Collections;// 使用 Spline


public class MarbleManager : MonoBehaviour
{
    public static MarbleManager Instance; // 单例模式，方便其他脚本访问

    [Header("数据资源")]
    public TextAsset idiomFile;          // 拖入你的 chengyu.txt 文件
    public GameObject marblePrefab;      // 拖入你的 Marble Prefab
    public SplineContainer pathSpline;   // 拖入场景中的 Path 对象

    [Header("游戏参数")]
    public float marbleSpeed = 1f;
    public float marbleSpacing = 0.5f; // 珠子之间的距离

    [Header("数据存储")]
    private HashSet<string> idiomDictionary = new HashSet<string>();
    private List<Marble> marbleChain = new List<Marble>();
    private List<string> idiomList = new List<string>(); // 所有成语
    private List<char> allCharacters = new List<char>(); // 所有成语的字
    private List<char> shuffledCharacters = new List<char>(); // 打乱后的字


    void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        LoadIdioms();
    }

    void Start()
    {
        // 游戏开始时，生成初始的一串珠子
        StartCoroutine(SpawnInitialMarbles());


    }

    void Update()
    {
        // 每帧移动珠子链
        MoveMarbleChain();
    }

    void LoadIdioms()
    {
        if (idiomFile == null)
        {
            Debug.LogError("成语词典文件未设置!");
            return;
        }

        string[] lines = idiomFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Debug.Log($"加载成语文件，共 {lines.Length} 行。");
        foreach (string line in lines)
        {
            string idiom = line.Trim();
            if (idiom.Length == 4)
            {
                idiomDictionary.Add(idiom);
                idiomList.Add(idiom);
                foreach (char c in idiom)
                    allCharacters.Add(c);
            }
        }
        Debug.Log($"加载了 {idiomDictionary.Count} 个成语。");

        // 打乱所有字
        shuffledCharacters = new List<char>(allCharacters);
        ShuffleList(shuffledCharacters);
    }

    // Fisher-Yates 洗牌算法
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    //
    private IEnumerator SpawnInitialMarbles()
    {
        // 随机选一个成语作为初始珠子
        List<string> idioms = new List<string>(idiomDictionary);
        string randomIdiom = idioms[Random.Range(0, idioms.Count)];

        for (int i = 0; i < 20; i++) // 生成20个初始珠子
        {
            GameObject newMarbleObj = Instantiate(marblePrefab);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();

            // 从随机成语中循环取字
            newMarble.SetCharacter(randomIdiom[i % 4]);

            // 将新珠子添加到链条的头部
            marbleChain.Insert(0, newMarble);

            // 重新计算所有珠子的位置
            UpdateAllMarblePositions();

            yield return new WaitForSeconds(1f); // 逐个生成的效果
        }
    }

    // 根据珠子在链条中的位置，计算它在轨道上的具体坐标
    public void UpdateAllMarblePositions()
    {
        float currentDistance = 0;
        for (int i = 0; i < marbleChain.Count; i++)
        {
            Marble marble = marbleChain[i];
            marble.indexInChain = i;

            // distance / totalLength = a value between 0 and 1
            marble.positionOnPath = pathSpline.Spline.ConvertIndexUnit(currentDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            // 设置GameObject的位置和朝向
            Vector3 position = pathSpline.EvaluatePosition(marble.positionOnPath);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(marble.positionOnPath);
            marble.transform.position = position;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward); // 让珠子朝向正确

            currentDistance += marbleSpacing;
        }
    }

    private void MoveMarbleChain()
    {
        if (marbleChain.Count == 0) return;

        // 所有珠子都向前移动相同的距离
        float distanceToMove = marbleSpeed * Time.deltaTime;

        // 从链条尾部开始更新，避免位置计算错误
        for (int i = marbleChain.Count - 1; i >= 0; i--)
        {
            Marble marble = marbleChain[i];

            // 将当前位置（0-1的比例）转换成实际距离
            float currentDistance = pathSpline.Spline.ConvertIndexUnit(marble.positionOnPath, PathIndexUnit.Normalized, PathIndexUnit.Distance);

            // 新的距离
            float newDistance = currentDistance + distanceToMove;

            // 更新路径位置
            marble.positionOnPath = pathSpline.Spline.ConvertIndexUnit(newDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            // 更新GameObject的Transform
            Vector3 position = pathSpline.EvaluatePosition(marble.positionOnPath);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(marble.positionOnPath);
            marble.transform.position = position;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward);
        }
    }


    #region 新方法
    // 生成指定数量的珠子，并将它们添加到链条中
    public IEnumerator SpawnMarbles(int count, float spawnDelay = 0.1f)
    {
        float spawnDistance = 0f; // 出生点在路径起点
        for (int i = 0; i < count; i++)
        {
            if (shuffledCharacters.Count == 0)
                yield break;

            GameObject newMarbleObj = Instantiate(marblePrefab);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();
            newMarble.SetCharacter(shuffledCharacters[0]);
            shuffledCharacters.RemoveAt(0);

            // 插入到链尾
            marbleChain.Add(newMarble);

            // 设置初始位置
            float normalizedPos = pathSpline.Spline.ConvertIndexUnit(spawnDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            newMarble.positionOnPath = normalizedPos;
            Vector3 position = pathSpline.EvaluatePosition(normalizedPos);
            newMarble.transform.position = position;

            // 可选：逐步插值到目标位置实现顺滑
            yield return StartCoroutine(MoveMarbleToPosition(newMarble, normalizedPos, i * marbleSpacing));

            UpdateAllMarblePositions();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // 珠子顺滑移动到目标距离
    private IEnumerator MoveMarbleToPosition(Marble marble, float startNorm, float targetDistance)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startDistance = pathSpline.Spline.ConvertIndexUnit(startNorm, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        float endDistance = startDistance + targetDistance;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curDistance = Mathf.Lerp(startDistance, endDistance, t);
            float norm = pathSpline.Spline.ConvertIndexUnit(curDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            marble.positionOnPath = norm;
            Vector3 pos = pathSpline.EvaluatePosition(norm);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(norm);
            marble.transform.position = pos;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward);
            yield return null;
        }
    }


    #endregion

    // --- 核心消除逻辑 ---
    public void CheckForMatches(int insertionIndex)
    {
        // TODO
    }
}