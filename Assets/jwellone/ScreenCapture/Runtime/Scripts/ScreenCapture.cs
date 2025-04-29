using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Cysharp.Threading.Tasks;
using Unity.Collections;

#nullable enable

namespace jwellone
{
    public static class ScreenCapture
    {
        static IScreenshot? _screenshot;
        readonly static IScreenshot _defaultScreenshot;

        public static IScreenshot screenshot
        {
            get => _screenshot!;
            set
            {
                _screenshot = value;
                _screenshot ??= _defaultScreenshot;
            }
        }

        static ScreenCapture()
        {
            _defaultScreenshot = SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None && !SystemInfo.supportsAsyncGPUReadback ?
            new ReadPixelsScreenshot() : SystemInfo.supportsAsyncGPUReadback ? new AsyncGPUReadbackScreenshot() : new GraphicsCopyTextureScreenshot();
            _screenshot = _defaultScreenshot;
            Debug.Log($"[ScreenCapture]default({_defaultScreenshot.GetType().Name}) , copyTextureSupport({SystemInfo.copyTextureSupport}) , supportsAsyncGPUReadback({SystemInfo.supportsAsyncGPUReadback})");
        }

        public static async UniTask<bool> SaveAsync(string path, int width, int height, byte[] data, GraphicsFormat graphicsFormat, CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();

                var bytes = Path.GetExtension(path).ToLower() switch
                {
                    ".jpg" => ImageConversion.EncodeArrayToJPG(data, graphicsFormat, (uint)width, (uint)height),
                    ".jpeg" => ImageConversion.EncodeArrayToJPG(data, graphicsFormat, (uint)width, (uint)height),
                    ".tga" => ImageConversion.EncodeArrayToTGA(data, graphicsFormat, (uint)width, (uint)height),
                    _ => ImageConversion.EncodeArrayToPNG(data, graphicsFormat, (uint)width, (uint)height),
                };

                await File.WriteAllBytesAsync(path, bytes, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            return false;
        }

        public static async UniTask<bool> SaveAsync(string path, int width, int height, NativeArray<byte> data, GraphicsFormat graphicsFormat, CancellationToken token)
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                using var bytes = Path.GetExtension(path).ToLower() switch
                {
                    ".jpg" => ImageConversion.EncodeNativeArrayToJPG(data, graphicsFormat, (uint)width, (uint)height),
                    ".jpeg" => ImageConversion.EncodeNativeArrayToJPG(data, graphicsFormat, (uint)width, (uint)height),
                    ".tga" => ImageConversion.EncodeNativeArrayToTGA(data, graphicsFormat, (uint)width, (uint)height),
                    _ => ImageConversion.EncodeNativeArrayToPNG(data, graphicsFormat, (uint)width, (uint)height),
                };
                await UniTask.SwitchToMainThread();

                await File.WriteAllBytesAsync(path, bytes.ToArray(), token);
                return true;
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            return false;
        }

        public static void Render(Camera target, RenderTexture dest)
        {
            var tmpRT = target.targetTexture;
            target.targetTexture = dest;
            target.Render();
            target.targetTexture = tmpRT;
        }
    }
}
