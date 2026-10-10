using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class MenuPreviewCapture
{
    public static void Save(Canvas canvas, string filename)
    {
        string directory = Path.GetFullPath("Library/GameMenuPreview");
        Directory.CreateDirectory(directory);
        // Batchmode не рисует Game View. Рендерим настоящий Canvas отдельной камерой URP.

        var transforms = canvas.GetComponentsInChildren<Transform>(true);
        var layers = new int[transforms.Length];
        var cameraRoot = new GameObject("PauseMenuChecks — PreviewCamera", typeof(Camera));
        var camera = cameraRoot.GetComponent<Camera>();
        var target = new RenderTexture(1600, 900, 24);
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        var previousCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        var scaler = canvas.GetComponent<CanvasScaler>();
        bool scalerEnabled = scaler.enabled;
        float previousScale = canvas.scaleFactor;
        try
        {
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.10f, 0.08f);
            camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0f, 0f, -1000f);
            camera.targetTexture = target;
            for (int i = 0; i < transforms.Length; i++)
            {
                layers[i] = transforms[i].gameObject.layer;
                transforms[i].gameObject.layer = 31;
            }
            scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.scaleFactor = 1600f / 1920f;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest
            {
                destination = target
            });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(directory, filename), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousPlaneDistance;
            canvas.scaleFactor = previousScale;
            scaler.enabled = scalerEnabled;
            for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
            UnityEngine.Object.DestroyImmediate(cameraRoot);
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Canvas.ForceUpdateCanvases();
        }
    }

}
