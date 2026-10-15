using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BattleTv.Menu
{
    /// <summary>
    /// Fits the 1280×720 plate to the window. Wider than 16:9 crops the top and
    /// bottom equally. Narrower than 16:9 letterboxes. The still, the resting
    /// loop, and the one-shot clip share that rect.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        public const float PlateWidth = 1280f;
        public const float PlateHeight = 720f;
        public const float FadeSeconds = 0.25f;
        public const string ClipFileName = "Vale-vs-Brute-push.mp4";
        public const string RestFileName = "Vale-vs-Brute-rest.mp4";

        public readonly struct FitResult
        {
            public readonly Rect Drawn;
            public readonly Rect Window;
            public readonly Rect Uv;

            public FitResult(Rect drawn, Rect window, Rect uv)
            {
                Drawn = drawn;
                Window = window;
                Uv = uv;
            }
        }

        [SerializeField] RectTransform plate;
        [SerializeField] RawImage restImage;
        [SerializeField] VideoPlayer rest;
        [SerializeField] RawImage videoImage;
        [SerializeField] VideoPlayer video;

        RenderTexture _rest;
        RenderTexture _clip;
        bool _fading;
        float _fade;
        int _appliedW = -1;
        int _appliedH = -1;

        public static FitResult Fit(float windowW, float windowH)
        {
            var window = new Rect(0f, 0f, windowW, windowH);
            float drawnW = windowW;
            float drawnH = PlateHeight * (windowW / PlateWidth);
            float y = (windowH - drawnH) * 0.5f;
            var drawn = new Rect(0f, y, drawnW, drawnH);

            float bottom = Mathf.Max(drawn.yMin, window.yMin);
            float top = Mathf.Min(drawn.yMax, window.yMax);
            float visibleH = Mathf.Max(0f, top - bottom);
            float uvY = drawnH <= 0f ? 0f : (bottom - drawn.y) / drawnH;
            float uvH = drawnH <= 0f ? 0f : visibleH / drawnH;
            var uv = new Rect(0f, uvY, 1f, uvH);
            return new FitResult(drawn, window, uv);
        }

        public static Rect LeftFighterThird => new Rect(0f, 0f, PlateWidth / 3f, PlateHeight);

        public static Rect RightFighterThird => new Rect(PlateWidth * 2f / 3f, 0f, PlateWidth / 3f, PlateHeight);

        public static Rect MapToWindow(FitResult fit, Rect platePixels)
        {
            float x = fit.Drawn.x + (platePixels.x / PlateWidth) * fit.Drawn.width;
            float y = fit.Drawn.y + (platePixels.y / PlateHeight) * fit.Drawn.height;
            float w = (platePixels.width / PlateWidth) * fit.Drawn.width;
            float h = (platePixels.height / PlateHeight) * fit.Drawn.height;
            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// The third keeps its full width inside the window, and its vertical
        /// centre stays on screen. A side crop fails this. The equal top and
        /// bottom crop on a wide window does not.
        /// </summary>
        public static bool FighterThirdStaysVisible(FitResult fit, Rect platePixels)
        {
            Rect mapped = MapToWindow(fit, platePixels);
            float left = Mathf.Max(mapped.xMin, fit.Window.xMin);
            float right = Mathf.Min(mapped.xMax, fit.Window.xMax);
            float bottom = Mathf.Max(mapped.yMin, fit.Window.yMin);
            float top = Mathf.Min(mapped.yMax, fit.Window.yMax);
            bool fullWidth = (right - left) >= mapped.width - 0.01f;
            bool someHeight = (top - bottom) > 0.01f;
            float mid = mapped.y + (mapped.height * 0.5f);
            bool centreInside = mid >= fit.Window.yMin - 0.01f && mid <= fit.Window.yMax + 0.01f;
            return fullWidth && someHeight && centreInside;
        }

        public static string ClipUrl()
        {
            return MediaUrl(ClipFileName);
        }

        public static string RestUrl()
        {
            return MediaUrl(RestFileName);
        }

        public static string MediaUrl(string fileName)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, "menu", fileName);
        }

        void Awake()
        {
            _rest = Bind(rest, RestUrl(), true, restImage, false);
            _clip = Bind(video, ClipUrl(), false, videoImage, true);

            var button = GetComponentInChildren<Button>(true);
            if (button != null && EventSystemsSelected() == null)
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }

        static GameObject EventSystemsSelected()
        {
            var system = UnityEngine.EventSystems.EventSystem.current;
            return system == null ? null : system.currentSelectedGameObject;
        }

        void OnDestroy()
        {
            if (video != null)
            {
                video.loopPointReached -= OnClipEnded;
                video.errorReceived -= OnVideoError;
            }

            if (rest != null)
                rest.errorReceived -= OnRestError;

            Release(ref _clip);
            Release(ref _rest);
        }

        RenderTexture Bind(VideoPlayer player, string url, bool looping, RawImage image, bool fadeOnEnd)
        {
            if (player == null)
                return null;

            var texture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            texture.Create();
            player.source = VideoSource.Url;
            player.url = url;
            player.isLooping = looping;
            player.playOnAwake = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.aspectRatio = VideoAspectRatio.Stretch;
            player.targetTexture = texture;
            if (image != null)
            {
                image.texture = texture;
                image.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (fadeOnEnd)
            {
                player.loopPointReached += OnClipEnded;
                player.errorReceived += OnVideoError;
            }
            else
            {
                player.errorReceived += OnRestError;
            }

            return texture;
        }

        static void Release(ref RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        void Update()
        {
            ApplyFit();
            if (!_fading)
                return;

            _fade += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(_fade / FadeSeconds);
            SetVideoAlpha(alpha);
            if (alpha > 0f)
                return;

            _fading = false;
            if (videoImage != null)
                videoImage.gameObject.SetActive(false);
        }

        void ApplyFit()
        {
            if (plate == null)
                return;
            int w = Screen.width;
            int h = Screen.height;
            if (w < 1 || h < 1 || (w == _appliedW && h == _appliedH))
                return;

            FitResult fit = Fit(w, h);
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.zero;
            plate.pivot = Vector2.zero;
            plate.anchoredPosition = new Vector2(fit.Drawn.x, fit.Drawn.y);
            plate.sizeDelta = new Vector2(fit.Drawn.width, fit.Drawn.height);
            _appliedW = w;
            _appliedH = h;
        }

        void OnClipEnded(VideoPlayer source)
        {
            BeginFade();
        }

        void OnVideoError(VideoPlayer source, string message)
        {
            BeginFade();
        }

        void OnRestError(VideoPlayer source, string message)
        {
            if (restImage != null)
                restImage.gameObject.SetActive(false);
        }

        void BeginFade()
        {
            if (_fading || videoImage == null || !videoImage.gameObject.activeSelf)
                return;
            _fading = true;
            _fade = 0f;
        }

        void SetVideoAlpha(float alpha)
        {
            if (videoImage == null)
                return;
            Color color = videoImage.color;
            color.a = alpha;
            videoImage.color = color;
        }
    }
}
