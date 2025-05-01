using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

#nullable enable

namespace jwellone.Sample
{
    public class SampleScene : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] Camera _camera = null!;
        [SerializeField] Transform _cameraCenterPoint = null!;
        [SerializeField] Vector3 _cameraRotateAxis = Vector3.up;
        [SerializeField] float _cameraSpeedFactor = 0.1f;

        [Header("Cube")]
        [SerializeField] GameObject _prefabCube = null!;

        [Header("UI")]
        [SerializeField] Image _imgBg = null!;
        [SerializeField] Text _text = null!;
        [SerializeField] RawImage _rawImage = null!;
        [SerializeField] Image _imgSourceFpsBar = null!;

        int _cubeCount = 1;
        float _time;
        bool _executeScreenCapture = true;
        bool _executeCameraCapture = true;
        Texture2D? _cacheTexture;
        CancellationTokenSource? _cachCancellationTokenSource;
        readonly List<Image> _imgBars = new();
        readonly IScreenshot _readPixels = new ReadPixelsScreenshot();
        readonly IScreenshot _copyTexture = new GraphicsCopyTextureScreenshot();
        readonly IScreenshot _asyncGpuReadback = new AsyncGPUReadbackScreenshot();


        Texture2D? _texture
        {
            get => _cacheTexture;
            set
            {
                if (_cacheTexture != null)
                {
                    Destroy(_cacheTexture);
                }
                _cacheTexture = value;
            }
        }

        CancellationTokenSource? _cancellationTokenSource
        {
            get => _cachCancellationTokenSource;
            set
            {
                _cachCancellationTokenSource?.Cancel();
                _cachCancellationTokenSource?.Dispose();
                _cachCancellationTokenSource = value;
            }
        }


        void Awake()
        {
            Application.targetFrameRate = 60;

            for (var i = 0; i < 60; ++i)
            {
                var bar = Instantiate(_imgSourceFpsBar, _imgSourceFpsBar.transform.parent, false);
                bar.gameObject.SetActive(true);
                var rect = bar.rectTransform;
                var size = rect.sizeDelta;
                size.y = 0;
                rect.sizeDelta = size;
                _imgBars.Add(bar);
            }

            SetScreenCapture(true);
        }

        void Start()
        {
            var width = Screen.width;
            var height = Screen.height;
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _texture.Apply(false, false);

            var size = _rawImage.rectTransform.sizeDelta;
            size.x = Screen.width;
            size.y = Screen.height;
            _rawImage.rectTransform.sizeDelta = size;
            _rawImage.texture = _texture;

            var token = _cancellationTokenSource!.Token;
            UniTask.Void(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (_executeScreenCapture)
                    {
                        await ScreenCapture.screenshot.CopyAsync(_texture!, token);
                    }
                    else
                    {
                        await UniTask.WaitForEndOfFrame(this);
                    }
                }
            });

            UniTask.Void(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (_executeCameraCapture)
                    {
                        await ScreenCapture.screenshot.CopyAsync(_camera, _texture!, token);
                    }
                    else
                    {
                        await UniTask.WaitForEndOfFrame(this);
                    }
                }
            });

            UniTask.Void(async () =>
            {
                var path = Path.Combine(Application.persistentDataPath, "screen.jpg");
                while (!token.IsCancellationRequested)
                {
                    if (_executeScreenCapture)
                    {
                        await ScreenCapture.screenshot.SaveAsync(path, width, height, token);
                    }
                    else
                    {
                        await UniTask.WaitForEndOfFrame(this);
                    }
                }
            });

            UniTask.Void(async () =>
            {
                var path = Path.Combine(Application.persistentDataPath, "camera.jpg");
                while (!token.IsCancellationRequested)
                {
                    if (_executeCameraCapture)
                    {
                        await ScreenCapture.screenshot.SaveAsync(_camera, path, width, height, token);
                    }
                    else
                    {
                        await UniTask.WaitForEndOfFrame(this);
                    }
                }
            });
        }

        void OnEnable()
        {
            _cancellationTokenSource = new();
        }

        void OnDisable()
        {
            _cancellationTokenSource = null;
        }

        void OnDestroy()
        {
            _texture = null;
        }

        void Update()
        {
            CreateCubeIfNeeded();
            UpdateCamera();
            UpdateUI();
        }

        void CreateCubeIfNeeded()
        {
            if (Time.realtimeSinceStartup - _time > 0.1f)
            {
                var target = Instantiate(_prefabCube, null, false);
                target.transform.position = new Vector3(0, 3, 0);
                _time = Time.realtimeSinceStartup;
                ++_cubeCount;
            }
        }

        void UpdateCamera()
        {
            _camera.transform.RotateAround(
                _cameraCenterPoint.transform.position,
                _cameraRotateAxis,
                360.0f / (1.0f / _cameraSpeedFactor) * Time.deltaTime);
        }

        void UpdateUI()
        {
            var fps = 1f / Time.deltaTime;
            var sb = new StringBuilder();
            sb.Append("Frame Rate : ").AppendLine(fps.ToString("F1"));
            sb.Append("Cube : ").Append(_cubeCount.ToString());
            _text.text = sb.ToString();

            for (var i = _imgBars.Count - 1; i > 0; --i)
            {
                _imgBars[i].rectTransform.sizeDelta = _imgBars[i - 1].rectTransform.sizeDelta;
                _imgBars[i].color = _imgBars[i - 1].color;
            }

            var height = 64 * fps / 60.0f;
            var size = _imgBars[0].rectTransform.sizeDelta;
            size.y = height;
            _imgBars[0].rectTransform.sizeDelta = size;
            var h = _imgBars[0].rectTransform.sizeDelta.y;
            var color = Color.red;
            if (h >= 40)
            {
                color = Color.green;
            }
            else if (h >= 20)
            {
                color = Color.yellow;
            }

            _imgBars[0].color = color;
            color.a = 0.25f;
            _imgBg.color = color;
        }

        void SetScreenCapture(bool flag)
        {
            _executeScreenCapture = flag;
            _executeCameraCapture = !flag;
        }

        public void OnClickScreenCapture()
        {
            SetScreenCapture(true);
        }

        public void OnClickCameraCapture()
        {
            SetScreenCapture(false);
        }

        public void OnClickReadPixels()
        {
            ScreenCapture.screenshot = _readPixels;
        }

        public void OnClickCopyTexture()
        {
            ScreenCapture.screenshot = _copyTexture;
        }

        public void OnClickAsyncGpuReadback()
        {
            ScreenCapture.screenshot = _asyncGpuReadback;
        }
    }
}