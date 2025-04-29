using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

#nullable enable

namespace jwellone
{
    public sealed class ReadPixelsScreenshot : Screenshot
    {
        public override async UniTask<bool> SaveAsync(string path, int width, int height, CancellationToken token)
        {
            var texture = await CreateAsync(width, height, token);
            if (texture == null)
            {
                return false;
            }

            if (token.IsCancellationRequested)
            {
                Destroy(texture);
                return false;
            }

            var graphicsFormat = texture.graphicsFormat;
            var data = texture!.GetRawTextureData<byte>().ToArray();
            Destroy(texture);
            return await ScreenCapture.SaveAsync(path, width, height, data, graphicsFormat, token);
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
                dest.Apply();
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
            catch
            {
                Destroy(texture);
                return null;
            }
        }

        public override async UniTask CopyAsync(Camera target, Texture2D dest, CancellationToken token)
        {
            try
            {
                await OnCopyAsync(target, dest, token);
                dest.Apply();
            }
            catch
            {
                // Do Nothing.
            }
        }

        public override async UniTask<bool> SaveAsync(Camera target, string path, int width, int height, CancellationToken token)
        {
            var texture = await CreateAsync(target, width, height, token);
            if (texture == null)
            {
                return false;
            }

            if (token.IsCancellationRequested)
            {
                Destroy(texture);
                return false;
            }

            var graphicsFormat = texture.graphicsFormat;
            var data = texture!.GetRawTextureData<byte>().ToArray();
            Destroy(texture);
            return await ScreenCapture.SaveAsync(path, width, height, data, graphicsFormat, token);
        }

        async UniTask OnCopyAsync(Texture2D dest, CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

            var width = dest.width;
            var height = dest.height;
            var sourceRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var destRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(sourceRT);

            blit!.Blit(sourceRT, destRT);

            RenderTexture.ReleaseTemporary(sourceRT);
            var tmpRT = RenderTexture.active;
            RenderTexture.active = destRT;

            dest.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            RenderTexture.ReleaseTemporary(destRT);
            RenderTexture.active = tmpRT;
            //dest.Apply();
        }

        async UniTask OnCopyAsync(Camera target, Texture2D dest, CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

            var width = dest.width;
            var height = dest.height;
            var destRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

            ScreenCapture.Render(target, destRT);

            var tmpRT = RenderTexture.active;
            RenderTexture.active = destRT;
            dest.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = tmpRT;
            RenderTexture.ReleaseTemporary(destRT);
            //dest.Apply();
        }
    }
}