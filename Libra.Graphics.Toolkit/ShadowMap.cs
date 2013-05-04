#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ShadowMap : IDisposable
    {
        [Flags]
        enum DirtyFlags
        {
            LightCameras    = (1 << 0),
            RenderTargets   = (1 << 1),
            GaussianBlur    = (1 << 2),
        }

        public delegate LightCamera CreateLightCameraCallback();

        public delegate void DrawShadowCastersCallback(Camera camera, ShadowMapEffect effect);

        PSSMCameras cameras;

        CreateLightCameraCallback createLightCamera;

        LightCamera[] lightCameras;

        BoundingBox sceneBox;

        DrawShadowCastersCallback drawShadowCasters;

        RenderTarget[] renderTargets;

        Texture2D[] textures;

        ShadowMapEffect shadowMapEffect;

        GaussianBlur blur;

        int size;

        Vector3 lightDirection;

        DirtyFlags dirtyFlags;

        public Device Device { get; private set; }

        public int SplitCount
        {
            get { return cameras.SplitCount; }
            set
            {
                if (cameras.SplitCount == value) return;

                cameras.SplitCount = value;

                dirtyFlags |= DirtyFlags.RenderTargets;
            }
        }

        public CreateLightCameraCallback CreateLightCamera
        {
            get { return createLightCamera; }
            set
            {
                if (createLightCamera == value) return;

                createLightCamera = value;

                dirtyFlags |= DirtyFlags.LightCameras;
            }
        }

        public DrawShadowCastersCallback DrawShadowCasters
        {
            get { return drawShadowCasters; }
            set { drawShadowCasters = value; }
        }

        public ShadowMapForm Form
        {
            get { return shadowMapEffect.Form; }
            set
            {
                if (shadowMapEffect.Form == value) return;

                var previous = shadowMapEffect.Form;

                shadowMapEffect.Form = value;

                // VSM に関する切り替えならばレンダ ターゲットの再作成が必要。
                if (previous == ShadowMapForm.Variance ||
                    shadowMapEffect.Form == ShadowMapForm.Variance)
                {
                    dirtyFlags |= DirtyFlags.RenderTargets;
                }
            }
        }

        public int Size
        {
            get { return size; }
            set
            {
                if (size == value) return;

                size = value;

                dirtyFlags |= DirtyFlags.RenderTargets | DirtyFlags.GaussianBlur;
            }
        }

        public int BlurRadius { get; set; }

        public int BlurAmount { get; set; }

        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set
            {
                if (value.IsZero()) throw new ArgumentException("Light direction must be not zero.", "value");

                lightDirection = value;
                lightDirection.Normalize();
            }
        }

        public ShadowMap(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            shadowMapEffect = new ShadowMapEffect(device);

            cameras = new PSSMCameras();

            lightCameras = new LightCamera[PSSMCameras.MaxSplitCount];
            renderTargets = new RenderTarget[PSSMCameras.MaxSplitCount];
            textures = new Texture2D[PSSMCameras.MaxSplitCount];

            dirtyFlags |= DirtyFlags.LightCameras | DirtyFlags.RenderTargets | DirtyFlags.GaussianBlur;
        }

        public void Prepare(Matrix view, Matrix projection, BoundingBox sceneBox)
        {
            this.sceneBox = sceneBox;

            cameras.Update(view, projection, sceneBox);
        }

        public Camera GetCamera(int index)
        {
            return cameras[index];
        }

        public float[] GetSplitDistances()
        {
            return cameras.GetSplitDistances();
        }

        public void GetSplitDistances(float[] results)
        {
            cameras.GetSplitDistances(results);
        }

        public void Draw(DeviceContext context)
        {
            PrepareLightCameras();
            PrepareRenderTargets();
            PrepareGaussianBlur();

            context.DepthStencilState = DepthStencilState.Default;
            context.BlendState = BlendState.Opaque;

            // 各分割カメラについて描画。
            for (int i = 0; i < cameras.SplitCount; i++)
            {
                var camera = cameras[i];
                var lightCamera = lightCameras[i];

                // ライト カメラを更新。
                lightCamera.LightDirection = lightDirection;
                lightCamera.Update(camera.View, camera.Projection, sceneBox);

                // エフェクトを設定。
                shadowMapEffect.View = lightCamera.View;
                shadowMapEffect.Projection = lightCamera.Projection;

                // 描画
                var renderTarget = renderTargets[i];

                context.SetRenderTarget(renderTarget.GetRenderTargetView());
                context.Clear(Color.White);

                if (drawShadowCasters != null)
                {
                    // 現在の分割カメラに関する描画をコールバック。
                    // 分割カメラに含まれる投影オブジェクトの選別は、
                    // コールバックされる側のクラスで決定。
                    drawShadowCasters(camera, shadowMapEffect);
                }

                context.SetRenderTarget(null);

                // VSM ならばブラー。
                if (shadowMapEffect.Form == ShadowMapForm.Variance && blur != null)
                {
                    blur.Filter(
                        context,
                        renderTarget.GetShaderResourceView(),
                        renderTarget.GetRenderTargetView());
                }

                textures[i] = renderTarget;
            }
        }

        public LightCamera GetLightCamera(int index)
        {
            if ((uint) cameras.SplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return lightCameras[index];
        }

        public Texture2D GetTexture(int index)
        {
            if ((uint) cameras.SplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return textures[index];
        }

        void PrepareLightCameras()
        {
            if ((dirtyFlags & DirtyFlags.LightCameras) != 0)
            {
                Array.Clear(lightCameras, 0, lightCameras.Length);

                for (int i = 0; i < cameras.SplitCount; i++)
                {
                    if (createLightCamera == null)
                    {
                        lightCameras[i] = new BasicLightCamera();
                    }
                    else
                    {
                        lightCameras[i] = createLightCamera();
                    }
                }

                dirtyFlags &= ~DirtyFlags.LightCameras;
            }
        }

        void PrepareRenderTargets()
        {
            if ((dirtyFlags & DirtyFlags.RenderTargets) != 0)
            {
                for (int i = 0; i < renderTargets.Length; i++)
                {
                    if (renderTargets[i] != null)
                    {
                        renderTargets[i].Dispose();
                        renderTargets[i] = null;
                    }
                }

                Array.Clear(textures, 0, textures.Length);

                var format = SurfaceFormat.Single;
                if (shadowMapEffect.Form == ShadowMapForm.Variance)
                    format = SurfaceFormat.Vector2;

                // ブラーをかける場合があるので RenderTargetUsage.Preserve。
                for (int i = 0; i < cameras.SplitCount; i++)
                {
                    renderTargets[i] = Device.CreateRenderTarget();
                    renderTargets[i].Width = size;
                    renderTargets[i].Height = size;
                    renderTargets[i].Format = format;
                    renderTargets[i].RenderTargetUsage = RenderTargetUsage.Preserve;
                    renderTargets[i].Name = "ShadowMap" + i;
                    renderTargets[i].Initialize();
                }

                dirtyFlags &= ~DirtyFlags.RenderTargets;
            }
        }

        void PrepareGaussianBlur()
        {
            if (shadowMapEffect.Form != ShadowMapForm.Variance)
                return;

            if ((dirtyFlags & DirtyFlags.GaussianBlur) != 0)
            {
                if (blur != null)
                {
                    blur.Dispose();
                }

                blur = new GaussianBlur(Device, size, size, SurfaceFormat.Vector2);

                dirtyFlags &= ~DirtyFlags.GaussianBlur;
            }

            blur.Radius = BlurRadius;
            blur.Amount = BlurAmount;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~ShadowMap()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                shadowMapEffect.Dispose();
                
                if (blur != null)
                    blur.Dispose();

                foreach (var renderTarget in renderTargets)
                {
                    if (renderTarget != null)
                        renderTarget.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
