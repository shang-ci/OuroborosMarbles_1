using UnityEngine;

/// <summary>
/// 声音数据结构
/// </summary>
[System.Serializable]
public class SoundData
{
    public string id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
}
