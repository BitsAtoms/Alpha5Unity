using System;
using System.Collections.Concurrent;
using UnityEngine;

public class UnityMainThreadQueue : MonoBehaviour
{
    static readonly ConcurrentQueue<Action> Q = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("[UnityMainThreadQueue]");
        DontDestroyOnLoad(go);
        go.AddComponent<UnityMainThreadQueue>();
    }

    public static void Enqueue(Action a)
    {
        if (a != null) Q.Enqueue(a);
    }

    void Update()
    {
        while (Q.TryDequeue(out var a)) a();
    }
}
