using System.Threading;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;

#nullable enable

namespace jwellone
{
    public sealed class AsyncGPUReadbackScreenshot : Screenshot
    {
        readonly IScreenshot _copy = new GraphicsCopyTextureScreenshot();

        public async override UniTask<bool> SaveAsync(string path, int width, int height, CancellationToken token)
        {
            RenderTexture? destRT = null;

            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

                var sourceRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                destRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

                UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(sourceRT);

                blit!.Blit(sourceRT, destRT);

                RenderTexture.ReleaseTemporary(sourceRT);

                return await RequestAndSaveAsync(path, destRT, token);
            }
            finally
            {
                if (destRT != null)
                {
                    RenderTexture.ReleaseTemporary(destRT);
                }
            }
        }

        public override async UniTask<Texture2D?> CreateAsync(int width, int height, CancellationToken token)
        {
            return await _copy.CreateAsync(width, height, token);
        }

        public override async UniTask CopyAsync(Texture2D dest, CancellationToken token)
        {
            RenderTexture? destRT = null;
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

                var sourceRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                destRT = RenderTexture.GetTemporary(dest.width, dest.height, 0, RenderTextureFormat.ARGB32);

                UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(sourceRT);

                blit!.Blit(sourceRT, destRT);

                RenderTexture.ReleaseTemporary(sourceRT);

                var request = await AsyncGPUReadback.Request(destRT, 0, destRT.graphicsFormat);
                if (!token.IsCancellationRequested && !request.hasError)
                {
                    dest.LoadRawTextureData(request.GetData<Color32>());
                    dest.Apply();
                }
            }
            finally
            {
                if (destRT != null)
                {
                    RenderTexture.ReleaseTemporary(destRT);
                }
            }
        }

        public override async UniTask<Texture2D?> CreateAsync(Camera target, int width, int height, CancellationToken token)
        {
            return await _copy.CreateAsync(target, width, height, token);
        }

        public override async UniTask CopyAsync(Camera target, Texture2D dest, CancellationToken token)
        {
            RenderTexture? destRT = null;
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

                destRT = RenderTexture.GetTemporary(dest.width, dest.height, 24, RenderTextureFormat.ARGB32);
                ScreenCapture.Render(target, destRT);

                var request = await AsyncGPUReadback.Request(destRT, 0, destRT.graphicsFormat);
                if (!token.IsCancellationRequested && !request.hasError)
                {
                    dest.LoadRawTextureData(request.GetData<Color32>());
                    dest.Apply();
                }
            }
            finally
            {
                if (destRT != null)
                {
                    RenderTexture.ReleaseTemporary(destRT);
                }
            }
        }

        public override async UniTask<bool> SaveAsync(Camera target, string path, int width, int height, CancellationToken token)
        {
            RenderTexture? destRT = null;

            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);

                destRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                ScreenCapture.Render(target, destRT);
                return await RequestAndSaveAsync(path, destRT, token);
            }
            finally
            {
                if (destRT != null)
                {
                    RenderTexture.ReleaseTemporary(destRT);
                }
            }
        }

        async UniTask<bool> RequestAndSaveAsync(string path, RenderTexture sourceRT, CancellationToken token)
        {
            var width = sourceRT.width;
            var height = sourceRT.height;
            var buffer = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            try
            {
                var request = await AsyncGPUReadback.RequestIntoNativeArray(ref buffer, sourceRT, 0);
                if (request.hasError || token.IsCancellationRequested)
                {
                    return false;
                }

                return await ScreenCapture.SaveAsync(path, width, height, buffer, sourceRT.graphicsFormat, token);
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }
}
