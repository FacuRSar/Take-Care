using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public AudioSource source;

    [Header("UI Sounds")]
    public AudioClip select;
    public AudioClip back;
    public AudioClip hover;
    public AudioClip play;

    public void PlaySelect()
    {
        source.PlayOneShot(select);
    }

    public void PlayBack()
    {
        source.PlayOneShot(back);
    }

    public void PlayHover()
    {
        source.PlayOneShot(hover);
    }

    public void PlayPlay()
    {
        source.PlayOneShot(play);
    }
}
