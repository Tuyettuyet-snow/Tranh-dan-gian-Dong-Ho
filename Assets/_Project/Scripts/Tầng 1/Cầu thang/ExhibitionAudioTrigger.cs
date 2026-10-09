using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ExhibitionAudioTrigger : MonoBehaviour
{
    [Header("Âm thanh khu triển lãm (Tầng 2)")]
    public AudioClip exhibitionSound;
    public bool loopSound = true;

    private AudioSource audioSource;
    private bool isPlayerInside = false; // Tránh gọi phát nhạc liên tục

    private void Start()
    {
        // Tự động gắn AudioSource nếu chưa có
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = exhibitionSound;
        audioSource.loop = loopSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D (Nghe rõ ở mọi hướng)

        // Đảm bảo BoxCollider của cái cửa/vùng kích hoạt là Trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    // Khi người chơi BƯỚC VÀO khu vực
    private void OnTriggerEnter(Collider other)
    {
        // Đảm bảo chỉ người chơi mới kích hoạt (Nhớ gắn Tag "Player" cho XRRig)
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("Vào khu triển lãm - PHÁT NHẠC!");
            }
        }
    }

    // Khi người chơi BƯỚC RA KHỎI khu vực (xuống lầu)
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                Debug.Log("Rời khu triển lãm - TẮT NHẠC!");
            }
        }
    }
}