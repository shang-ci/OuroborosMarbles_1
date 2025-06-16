using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// “Ù∆µ‘¥≥ÿ
/// </summary>
public class AudioSourcePool
{
    private Queue<AudioSource> pool = new Queue<AudioSource>();
    [SerializeField] private Transform parent;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentTransform"></param>
    /// <param name="initialSize"></param>
    public AudioSourcePool(Transform parentTransform, int initialSize = 10)
    {
        parent = parentTransform;
        for (int i = 0; i < initialSize; i++)
        {
            AddSource();
        }
    }

    private void AddSource()
    {
        GameObject obj = new GameObject("PooledAudioSource");
        obj.transform.SetParent(parent);
        obj.SetActive(false);
        AudioSource source = obj.AddComponent<AudioSource>();
        pool.Enqueue(source);
    }

    public AudioSource Get()
    {
        if (pool.Count == 0) AddSource();
        AudioSource source = pool.Dequeue();
        source.gameObject.SetActive(true);
        return source;
    }

    public void Release(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        pool.Enqueue(source);
    }
}
