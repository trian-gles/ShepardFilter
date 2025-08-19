using UnityEngine;

public class DummyWalking : MonoBehaviour
{
    private AudioSource footstepSound;
    private ShepardFilter.ShepardFilter filter;

    float offset = 0;
    public float offsetChange = 0.3f; // 0.0015 0.003 0.0045 0.006

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        footstepSound = GetComponent<AudioSource>();
        filter = GetComponent<ShepardFilter.ShepardFilter>();
        InvokeRepeating(nameof(Sound), 0.5f, 1f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {

    }

    void Sound()
    {
        filter.SetOffset(offset);
        footstepSound.Play();
        offset += offsetChange;
        offset %= 1.0f;

    }
}
