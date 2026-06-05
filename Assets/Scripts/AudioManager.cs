using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    [Header("Audio Clips")]
    public AudioClip[] backgroundMusics;
    public AudioClip clearSound;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && backgroundMusics.Length > 0)
        {
            audioSource.clip = backgroundMusics[0];
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchBGM(int index)
    {
        if (index >= 0 && index < backgroundMusics.Length)
        {
            if (audioSource != null)
            {
                audioSource.clip = backgroundMusics[index];
                audioSource.Play();
            }
        }
    }

    public void PlayClearSound()
    {
        if (audioSource != null && clearSound != null)
        {
            audioSource.PlayOneShot(clearSound);
        }
    }

    public void StopBGM()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
