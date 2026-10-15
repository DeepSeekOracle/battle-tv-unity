using UnityEngine;

namespace BattleTv.Menu
{
    /// <summary>
    /// Intro bed. Loops at a quarter volume. A later in-game player calls
    /// <see cref="SetMuted"/> when the show should take the speakers.
    /// </summary>
    public sealed class IntroMusic : MonoBehaviour
    {
        public const float IntroVolume = 0.25f;

        public static IntroMusic Current { get; private set; }

        [SerializeField] AudioSource source;

        void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            DontDestroyOnLoad(gameObject);
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source == null)
                return;

            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = IntroVolume;
            source.mute = false;
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            PlayIntro();
        }

        void Update()
        {
            if (source == null || source.mute || source.isPlaying)
                return;
            if (!Input.anyKeyDown && !Input.GetMouseButtonDown(0))
                return;
            PlayIntro();
        }

        public void PlayIntro()
        {
            if (source == null || source.clip == null || source.mute)
                return;
            source.volume = IntroVolume;
            source.loop = true;
            if (!source.isPlaying)
                source.Play();
        }

        public void SetMuted(bool muted)
        {
            if (source == null)
                return;
            source.mute = muted;
            if (!muted)
                PlayIntro();
        }
    }
}
