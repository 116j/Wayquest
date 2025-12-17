using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundController : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField]
    List<AudioClip> m_sounds;

    protected AudioSource m_audio;

    private void Start()
    {
        m_audio = GetComponent<AudioSource>();
    }
    /// <summary>
    /// Включить клип один раз
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySound(AudioClip clip)
    {
        m_audio.PlayOneShot(clip);
    }
    /// <summary>
    /// Включить клип на повторе
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySoundLoop(AudioClip clip)
    {
        m_audio.loop = true;
        if (m_audio.clip != clip)
        {
            m_audio.Stop();
            m_audio.clip = clip;
        }
        if (!m_audio.isPlaying)
        {
            m_audio.Play();
        }
    }
    /// <summary>
    /// Включить звук по имени
    /// </summary>
    /// <param name="sound"></param>
    public void PlaySound(string sound)
    {
        m_audio.PlayOneShot(m_sounds.Find(s => s.name == sound));
    }
    /// <summary>
    /// Прервать воспроизведение
    /// </summary>
    public void StopSound()
    {
        m_audio.loop = false;
        m_audio.Stop();
    }
}
