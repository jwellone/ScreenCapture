using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

#nullable enable

namespace jwellone
{
    public interface IScreenshotBlit
    {
        void Blit(RenderTexture source, RenderTexture dest);
    }

    public interface IScreenshot
    {
        IScreenshotBlit? blit { get; set; }
        UniTask<bool> SaveAsync(string path, int width, int height, CancellationToken token);
        UniTask<Texture2D?> CreateAsync(int width, int height, CancellationToken token);
        UniTask CopyAsync(Texture2D dest, CancellationToken token);
        UniTask<Texture2D?> CreateAsync(Camera target, int width, int height, CancellationToken token);
        UniTask CopyAsync(Camera target, Texture2D dest, CancellationToken token);
        UniTask<bool> SaveAsync(Camera target, string path, int width, int height, CancellationToken token);
    }

    public abstract class Screenshot : IScreenshot
    {
        class DefaultBlit : IScreenshotBlit
        {
            Material? _material;

            ~DefaultBlit()
            {
                if (_material != null)
                {
                    Destroy(_material);
                    _material = null;
                }
            }

            public void Blit(RenderTexture source, RenderTexture dest)
            {
                _material ??= Resources.Load<Material>("FlipVertical");
                Graphics.Blit(source, dest, _material);
            }
        }

        readonly static IScreenshotBlit _defaultBlit = new DefaultBlit();

        IScreenshotBlit? _blit;
        public IScreenshotBlit? blit
        {
            get => _blit ??= _defaultBlit;
            set => _blit = value;
        }

        public abstract UniTask<bool> SaveAsync(string path, int width, int height, CancellationToken token);
        public abstract UniTask<Texture2D?> CreateAsync(int width, int height, CancellationToken token);
        public abstract UniTask CopyAsync(Texture2D dest, CancellationToken token);
        public abstract UniTask<Texture2D?> CreateAsync(Camera target, int width, int height, CancellationToken token);
        public abstract UniTask CopyAsync(Camera target, Texture2D dest, CancellationToken token);
        public abstract UniTask<bool> SaveAsync(Camera target, string path, int width, int height, CancellationToken token);

        protected static void Destroy(UnityEngine.Object @object)
        {
#if UNITY_EDITOR
            GameObject.DestroyImmediate(@object);
#else
            GameObject.Destroy(@object);
#endif
        }
    }
}