using HideAndSeek.Core;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEffectManaager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] killClips;
    [SerializeField] private AudioClip[] AssertClips;
    [SerializeField] private AudioClip gameStart;
    [SerializeField] private AudioClip SirenShort;
    [SerializeField] private AudioClip MissionComplete;
    [SerializeField] private AudioClip UIClick;

    private AudioSource audioSource;

    // Singleton instance
    private static AudioEffectManaager _instance;
    public static AudioEffectManaager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioEffectManaager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioEffectManaager");
                    _instance = go.AddComponent<AudioEffectManaager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            InitComponents();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("AudioSource was missing and has been added automatically.");
        }
    }

    public void PlayKillEffect()
    {
        if (killClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned to AudioEffectManager.");
            return;
        }
        int randomIndex = Random.Range(0, killClips.Length);
        audioSource.PlayOneShot(killClips[randomIndex]);
    }

    public void PlayAssertEffect()
    {
        if (AssertClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned to AudioEffectManager.");
            return;
        }
        int randomIndex = Random.Range(0, AssertClips.Length);
        audioSource.PlayOneShot(AssertClips[randomIndex]);
    }

    public void PlayGameStartEffect()
    {
        if (gameStart == null)
        {
            Debug.LogWarning("No audio clip assigned to AudioEffectManager.");
            return;
        }
        audioSource.PlayOneShot(gameStart);
    }

    public void PlayGameStopEffect()
    {
        if (SirenShort == null)
        {
            Debug.LogWarning("No audio clip assigned to AudioEffectManager.");
            return;
        }
        audioSource.PlayOneShot(SirenShort);
    }

    public void PlayMissionCompleteEffect()
    {
        if (MissionComplete == null)
        {
            Debug.LogWarning("No audio clip assigned to AudioEffectManager.");
            return;
        }
        audioSource.PlayOneShot(MissionComplete);
    }
    public void PlayUIClickEffect()
    {
        if (UIClick == null)
        {
            Debug.LogWarning("No audio clip assigned to AudioEffectManager.");
            return;
        }
        audioSource.PlayOneShot(UIClick);
    }
}
