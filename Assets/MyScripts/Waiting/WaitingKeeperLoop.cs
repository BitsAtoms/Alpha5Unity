using UnityEngine;

public class WaitingKeeperLoop : MonoBehaviour
{
    public Animator animator;

    // nombres EXACTOS de los estados del Animator
    public string[] waitingStates = { "Waiting1", "Waiting2", "Waiting3" };

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        PlayRandomWaiting();
    }

    void PlayRandomWaiting()
    {
        if (waitingStates.Length == 0) return;

        int randomIndex = Random.Range(0, waitingStates.Length);
        string chosenState = waitingStates[randomIndex];

        animator.Play(chosenState, 0, 0f);
    }
}