using UnityEngine;

public class BabyHunger : MonoBehaviour
{
    [Tooltip("How fast the baby hunger drains per second")]
    public float drainPerSecond = 0.005f;

    [Range(0f, 1f)]
    public float hungerLevel = 1f; // 1 = full, 0 = starving
    public float hungerThreshold = 0.5f;

    private void Update()
    {
        hungerLevel = Mathf.Max(0f, hungerLevel - drainPerSecond * Time.deltaTime);
    }

    public bool IsHungry()
    {
        return hungerLevel < hungerThreshold;
    }

    public void Feed()
    {
        hungerLevel = 1f;
    }
}
