// IdiomDataManager.cs
// 职责：专门管理成语数据，包括加载、筛选、分发。实现数据与游戏逻辑的分离。

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于高效的随机排序

public class IdiomDataManager
{
    // [第一级数据]: 游戏所知的所有成语的总仓库
    private HashSet<string> _fullIdiomDictionary;

    // [第二级数据]: 为本局游戏特别挑选出的一组成语
    private List<string> _sessionIdioms;

    // [第三级数据]: 将本局成语拆分成的所有单个汉字
    private List<char> _sessionCharacterPool;

    // [第四级数据]: 打乱顺序后的汉字队列，用于玩家发射
    private Queue<char> _shuffledCharacterQueue;

    // [新数据结构] 专门用于生成初始珠子链的队列
    private Queue<char> _initialSpawnQueue;

    private List<char> _initialSpawnBackup;   // 初始生成队列的备份
    private List<char> _shootBackup;          // 发射队列的备份

    /// <summary>
    /// 构造函数，在游戏开始时初始化整个数据管道。
    /// </summary>
    /// <param name="idiomFile">包含所有成语的文本文件资源</param>
    /// <param name="idiomsForThisSession">为本局游戏挑选多少个成语</param>
    public IdiomDataManager(TextAsset idiomFile, int idiomsForThisSession)
    {
        // 步骤1: 加载所有成语到总仓库
        _fullIdiomDictionary = new HashSet<string>();
        LoadAllIdiomsFromFile(idiomFile);

        // 步骤2: 从总仓库中为本局游戏挑选成语
        _sessionIdioms = new List<string>();
        SelectIdiomsForSession(idiomsForThisSession);

        // 步骤3: 根据选中的成语，构建本局的“字池”
        _sessionCharacterPool = new List<char>();
        PopulateCharacterPool();

        // 步骤4: 将字池打乱，形成发射队列
        _shuffledCharacterQueue = new Queue<char>();
        _initialSpawnQueue = new Queue<char>();
        ShuffleAndDistributePools();
    }

    private void LoadAllIdiomsFromFile(TextAsset file)
    {
        if (file == null)
        {
            Debug.LogError("成语词典文件未提供！");
            return;
        }
        string[] lines = file.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (line.Trim().Length == 4)
            {
                _fullIdiomDictionary.Add(line.Trim());
            }
        }
    }

    private void SelectIdiomsForSession(int count)
    {
        List<string> allIdiomsList = _fullIdiomDictionary.ToList();
        // 如果请求的数量大于等于总数，则使用所有成语
        if (allIdiomsList.Count <= count)
        {
            _sessionIdioms = allIdiomsList;
            return;
        }

        // 随机挑选不重复的成语
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, allIdiomsList.Count);
            _sessionIdioms.Add(allIdiomsList[randomIndex]);
            allIdiomsList.RemoveAt(randomIndex);
        }

        foreach(var chengyu in _sessionIdioms)
        {
            Debug.Log($"本局成语：{chengyu}/n");
        }
    }

    private void PopulateCharacterPool()
    {
        foreach (string idiom in _sessionIdioms)
        {
            _sessionCharacterPool.AddRange(idiom.ToCharArray());
        }
    }

    /// <summary>
    /// [新方法] 将字池打乱，并按一定比例分配给“初始生成队列”和“发射队列”
    /// </summary>
    private void ShuffleAndDistributePools()
    {
        var shuffledList = _sessionCharacterPool.OrderBy(x => System.Guid.NewGuid()).ToList();

        // 例如，我们可以把前一半的字用于初始生成，后一半用于发射
        // 这个比例可以根据你的设计来调整
        int splitIndex = Mathf.CeilToInt(shuffledList.Count / 2f);

        // 备份
        _initialSpawnBackup = shuffledList.Take(splitIndex).ToList();
        // 备份：全部字都备份到_shootBackup
        _shootBackup = new List<char>(shuffledList);

        // 填充初始生成队列
        for (int i = 0; i < splitIndex; i++)
        {
            _initialSpawnQueue.Enqueue(shuffledList[i]);
        }

        // 填充发射队列：全部字
        _shuffledCharacterQueue = new Queue<char>(_shootBackup);

        Debug.Log($"数据初始化：初始生成队列 {_initialSpawnQueue.Count} 个字，发射队列 {_shuffledCharacterQueue.Count} 个字");
    }

    /// <summary>
    /// 打乱字池并填充发射队列
    /// </summary>
    private void ShuffleAndFillQueue()
    {
        // 使用 Linq 的 OrderBy 和 Guid 实现一个高效的随机排序
        var shuffledList = _sessionCharacterPool.OrderBy(x => System.Guid.NewGuid()).ToList();
        _shuffledCharacterQueue = new Queue<char>(shuffledList);
    }


    /// <summary>
    /// 检查一个字符串是否是本局游戏中的有效成语
    /// </summary>
    public bool IsValidSessionIdiom(string potentialIdiom)
    {
        // 只在小范围的本局成语中查找，性能高
        return _sessionIdioms.Contains(potentialIdiom);
    }

    /// <summary>
    /// 获取一个随机的成语，用于生成初始珠子链。
    /// </summary>
    public string GetRandomSessionIdiom()
    {
        if (_sessionIdioms.Count == 0) return "万事如意"; // 备用成语
        return _sessionIdioms[Random.Range(0, _sessionIdioms.Count)];
    }

    /// <summary>
    /// 从初始生成队列中取出一个字。
    /// </summary>
    public char GetNextCharacterForInitialSpawn()
    {
        if (_initialSpawnQueue.Count > 0)
        {
            return _initialSpawnQueue.Dequeue();
        }

        // 用备份重新打乱补充
        Debug.LogWarning("初始生成队列已空，重新打乱补充！");
        var reshuffled = _initialSpawnBackup.OrderBy(x => System.Guid.NewGuid()).ToList();
        _initialSpawnQueue = new Queue<char>(reshuffled);
        return _initialSpawnQueue.Dequeue();
    }

    /// <summary>
    /// 从发射队列中取出一个字，如果队列为空则自动重新填充
    /// </summary>
    public char GetNextCharacterToShoot()
    {
        if (_shuffledCharacterQueue.Count == 0)
        {
            Debug.LogWarning("发射队列已空，重新打乱补充！");
            var reshuffled = _shootBackup.OrderBy(x => System.Guid.NewGuid()).ToList();
            _shuffledCharacterQueue = new Queue<char>(reshuffled);
        }

        // Queue.Dequeue() 会自动取出并移除第一个元素
        return _shuffledCharacterQueue.Dequeue();
    }
}