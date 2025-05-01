using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

#nullable enable

namespace jwellone
{
    public sealed class GraphicsCopyTextureScreenshot : Screenshot
    {
        readonly ReadPixelsScreenshot _readPixles = new ReadPixelsScreenshot();

        public override async UniTask<bool> SaveAsync(string path, int width, int height, CancellationToken token)
        {
            return await _readPixles.SaveAsync(path, width, height, token);
        }

        public override async UniTask<Texture2D?> CreateAsync(int width, int height, CancellationToken token)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                await OnCopyAsync(texture, token);
                return texture;
            }
            catch
            {
                Destroy(texture);
                return null;
            }
        }

        public override async UniTask CopyAsync(Texture2D dest, CancellationToken token)
        {
            try
            {
                await OnCopyAsync(dest, token);
            }
            catch
            {
                // Do Nothing.
            }
        }

        public override async UniTask<Texture2D?> CreateAsync(Camera target, int width, int height, CancellationToken token)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                await OnCopyAsync(target, texture, token);
                return texture;
            }
            catch (OperationCanceledException)
            {
                Destroy(texture);
            }

            return null;
        }

        public override async UniTask CopyAsync(Camera target, Texture2D dest, CancellationToken token)
        {
            try
            {
                await OnCopyAsync(target, dest, token);
            }
            catch (OperationCanceledException)
            {
                // Do Nothing.
            }
        }

        public override async UniTask<bool> SaveAsync(Camera target, string path, int width, int height, CancellationToken token)
        {
            return await _readPixles.SaveAsync(target, path, width, height, token);
        }

        async UniTask OnCopyAsync(Texture2D dest, CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

            var sourceRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var destRT = RenderTexture.GetTemporary(dest.width, dest.height, 0, RenderTextureFormat.ARGB32);

            UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(sourceRT);

            blit!.Blit(sourceRT, destRT);
            Graphics.CopyTexture(destRT, dest);

            RenderTexture.ReleaseTemporary(sourceRT);
            RenderTexture.ReleaseTemporary(destRT);
        }

        async UniTask OnCopyAsync(Camera target, Texture2D dest, CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

            var destRT = RenderTexture.GetTemporary(dest.width, dest.height, 24, RenderTextureFormat.ARGB32);
            ScreenCapture.Render(target, destRT);
            Graphics.CopyTexture(destRT, dest);
            RenderTexture.ReleaseTemporary(destRT);
        }
    }
}
