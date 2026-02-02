using System;
using System.Collections.Concurrent;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> _q = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var go = new GameObject("[MainThreadDispatcher]");
        DontDestroyOnLoad(go);
        go.AddComponent<UnityMainThreadDispatcher>();
    }

    public static void Enqueue(Action a)
    {
        if (a != null) _q.Enqueue(a);
    }

    void Update()
    {
        while (_q.TryDequeue(out var a))
            a();
    }
}
