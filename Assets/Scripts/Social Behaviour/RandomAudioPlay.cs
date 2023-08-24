using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomAudioPlay : MonoBehaviour
{
    private AudioSource audioSource;
   [Range(0, 1)] 
    public float playProbability = 0.1f; 

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void TryPlayAudio()
    {
        if (Random.value < playProbability)
        {
            audioSource.Play();
        }
    }
}
