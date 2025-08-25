using UnityEngine;

public class DummyWalking : MonoBehaviour
{
    private AudioSource footstepSound;
    private ShepardFilter.ShepardFilter filter;
    float offset = 0;
    [SerializeField] public float offsetChange = 0.3f;

    void Start()
    {
        footstepSound = GetComponent<AudioSource>();
        filter = GetComponent<ShepardFilter.ShepardFilter>();
        InvokeRepeating(nameof(Sound), 0.5f, 1f);
    }

    void Sound()
    {
        filter.SetOffset(offset);
        footstepSound.Play();
        offset += offsetChange;
        offset %= 1.0f;

    }
}
