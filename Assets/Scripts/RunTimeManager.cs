using UnityEngine;

public class RunTimeManager : MonoBehaviour
{
    [Range(0f, 10f)]
    public float runTimeSpeed =1.0f;

    private void OnValidate() {
        Time.timeScale = runTimeSpeed;
    }
}