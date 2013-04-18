#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    //public sealed class ShadowMap : IDisposable
    //{
    //    #region Techniques

    //    public enum Techniques
    //    {
    //        /// <summary>
    //        /// 基礎的なシャドウ マッピング。
    //        /// </summary>
    //        Basic,

    //        /// <summary>
    //        /// VSM (Variant Shadow Mapping)。
    //        /// </summary>
    //        Vsm,

    //        /// <summary>
    //        /// PCF (Percentage Closer Filtering) 2x2 カーネル。
    //        /// </summary>
    //        //Pcf2x2,

    //        /// <summary>
    //        /// PCF (Percentage Closer Filtering) 3x3 カーネル。
    //        /// </summary>
    //        //Pcf3x3
    //    }

    //    #endregion

    //    #region Settings

    //    public sealed class Settings
    //    {
    //        // メモ
    //        //
    //        // VSM が最も綺麗な影となるが、最前面以外の分割視錐台で深度テストが上手くいっていない。
    //        // また、高い崖のような地形による投影において、ライト ブリーディングが激しい。
    //        // なお、分割数 1 で VSM を行うと、カメラ近隣はほとんどが影なしと判定される。
    //        //
    //        // Pcf は、3x3 程度なら Basic とそれ程変わりがない。
    //        //
    //        // 最も無難な設定が Basic であり、ライト ブリーディングを解決できるならば VSM。
    //        //

    //        public const int MinSplitCount = 1;

    //        public const int MaxSplitCount = 3;

    //        int size = 2048;

    //        Techniques technique = Techniques.Basic;

    //        SurfaceFormat format = SurfaceFormat.Single;

    //        float depthBias = 0.0005f;

    //        float farPlaneDistance = 1000.0f;

    //        int splitCount = 3;

    //        float splitLambda = 0.5f;

    //        /// <summary>
    //        /// シャドウ マップ生成方法の種類を取得または設定します。
    //        /// </summary>
    //        public ShadowMap.Techniques Technique
    //        {
    //            get { return technique; }
    //            set
    //            {
    //                technique = value;

    //                switch (technique)
    //                {
    //                    case ShadowMap.Techniques.Vsm:
    //                        format = SurfaceFormat.Vector2;
    //                        break;
    //                    default:
    //                        format = SurfaceFormat.Single;
    //                        break;
    //                }
    //            }
    //        }

    //        /// <summary>
    //        /// シャドウ マップのサイズを取得または設定します。
    //        /// </summary>
    //        public int Size
    //        {
    //            get { return size; }
    //            set
    //            {
    //                if (value < 1) throw new ArgumentOutOfRangeException("value");

    //                size = value;
    //            }
    //        }

    //        /// <summary>
    //        /// シャドウ マップの SurfaceFormat を取得します。
    //        /// シャドウ マップの SurfaceFormat はシャドウ マップ生成方法により決定され、
    //        /// Basic および Pcf の場合は SurfaceFormat.Single、
    //        /// Vsm の場合は SurfaceFormat.Vector2 となります。
    //        /// </summary>
    //        public SurfaceFormat Format
    //        {
    //            get { return format; }
    //        }

    //        /// <summary>
    //        /// シャドウ マップの深度バイアスを取得または設定します。
    //        /// シャドウ マップのサイズにより深度の精度が変わるため、適切な値はサイズにより異なります。
    //        /// より小さいサイズのシャドウ マップを用いる場合、より大きな深度バイアスが必要となります。
    //        /// </summary>
    //        public float DepthBias
    //        {
    //            get { return depthBias; }
    //            set
    //            {
    //                if (value < 0) throw new ArgumentOutOfRangeException("value");

    //                depthBias = value;
    //            }
    //        }

    //        /// <summary>
    //        /// シャドウ マップ描画で使用するカメラの FarPlaneDistance を取得または設定します。
    //        /// 遠方に描画するオブジェクトに対して投影する必要はほとんどないため、
    //        /// シーン描画で用いるカメラの FarPlaneDistance よりも短い距離となるように設定します。
    //        /// </summary>
    //        public float FarPlaneDistance
    //        {
    //            get { return farPlaneDistance; }
    //            set
    //            {
    //                if (value < 0) throw new ArgumentOutOfRangeException("value");

    //                farPlaneDistance = value;
    //            }
    //        }

    //        /// <summary>
    //        /// シャドウ マップ分割数を取得または設定します。
    //        /// </summary>
    //        public int SplitCount
    //        {
    //            get { return splitCount; }
    //            set
    //            {
    //                if (value < MinSplitCount || MaxSplitCount < value) throw new ArgumentOutOfRangeException("value");

    //                splitCount = value;
    //            }
    //        }

    //        /// <summary>
    //        /// シャドウ マップ分割ラムダ値を取得または設定します。
    //        /// 分割ラムダ値は、分割視錐台間での重なりの度合いを決定します。
    //        /// </summary>
    //        public float SplitLambda
    //        {
    //            get { return splitLambda; }
    //            set
    //            {
    //                if (value < 0 || 1 < value) throw new ArgumentOutOfRangeException("value");

    //                splitLambda = value;
    //            }
    //        }

    //        /// <summary>
    //        /// 分散シャドウ マップのブラー設定を取得します。
    //        /// </summary>
    //        public BlurSettings VsmBlur { get; private set; }

    //        public Settings()
    //        {
    //            VsmBlur = new BlurSettings();
    //        }
    //    }

    //    #endregion

    //    public const Techniques DefaultShadowMapTechnique = Techniques.Basic;

    //    Vector3[] corners = new Vector3[8];

    //    float[] splitDistances;

    //    float[] safeSplitDistances;

    //    LightCamera[] splitLightCameras;

    //    Matrix[] safeSplitLightViewProjections;

    //    RenderTarget[] splitRenderTargets;

    //    Texture2D[] safeSplitShadowMaps;

    //    Queue<ShadowCaster>[] splitShadowCasters;

    //    ShadowMapEffect shadowMapEffect;

    //    GaussianBlur blur;

    //    BoundingBox frustumBoundingBox;

    //    Settings settings;

    //    [Flags]
    //    enum DirtyFlags
    //    {
    //        RenderTargets   = (1 << 0),
    //        GaussianBlur    = (1 << 1),
    //        Projection      = (1 << 3),
    //    }

    //    DirtyFlags dirtyFlags;

    //    Techniques technique;

    //    int size;

    //    Matrix view;

    //    Matrix projection;

    //    float fov;

    //    float aspectRatio;

    //    float nearPlaneDistance;

    //    float farPlaneDistance;

    //    BoundingFrustum frustum;

    //    BoundingBox frustumBox;

    //    BoundingFrustum[] splitFrustums;

    //    public Techniques Technique
    //    {
    //        get { return technique; }
    //        set
    //        {
    //            if (technique == value) return;

    //            var lastTechnique = technique;

    //            technique = value;

    //            if (lastTechnique == Techniques.Vsm || technique == Techniques.Vsm)
    //                dirtyFlags |= DirtyFlags.RenderTargets;
    //        }
    //    }

    //    public int Size
    //    {
    //        get { return size; }
    //        set
    //        {
    //            if (size == value) return;

    //            size = value;

    //            dirtyFlags |= DirtyFlags.RenderTargets | DirtyFlags.GaussianBlur;
    //        }
    //    }

    //    public int BlurRadius { get; set; }

    //    public int BlurAmount { get; set; }

    //    public Matrix View
    //    {
    //        get { return view; }
    //        set { view = value; }
    //    }

    //    public float Fov
    //    {
    //        get { return fov; }
    //        set
    //        {
    //            if (fov == value) return;

    //            fov = value;
                
    //            dirtyFlags |= DirtyFlags.Projection;
    //        }
    //    }

    //    public float AspectRatio
    //    {
    //        get { return aspectRatio; }
    //        set
    //        {
    //            if (aspectRatio == value) return;

    //            aspectRatio = value;

    //            dirtyFlags |= DirtyFlags.Projection;
    //        }
    //    }

    //    public float NearPlaneDistance
    //    {
    //        get { return nearPlaneDistance; }
    //        set
    //        {
    //            if (nearPlaneDistance == value) return;

    //            nearPlaneDistance = value;

    //            dirtyFlags |= DirtyFlags.Projection;
    //        }
    //    }

    //    public float FarPlaneDistance
    //    {
    //        get { return farPlaneDistance; }
    //        set
    //        {
    //            if (farPlaneDistance == value) return;

    //            farPlaneDistance = value;
                
    //            dirtyFlags |= DirtyFlags.Projection;
    //        }
    //    }

    //    public Device Device { get; private set; }

    //    public int SplitCount { get; private set; }

    //    public float[] SplitDistances
    //    {
    //        get { return (float[]) splitDistances.Clone(); }
    //    }

    //    public Matrix[] SplitLightViewProjections
    //    {
    //        get
    //        {
    //            for (int i = 0; i < splitLightCameras.Length; i++)
    //                safeSplitLightViewProjections[i] = splitLightCameras[i].LightViewProjection;
    //            return safeSplitLightViewProjections;
    //        }
    //    }

    //    public Texture2D[] SplitShadowMaps
    //    {
    //        get { return (Texture2D[]) splitRenderTargets.Clone(); }
    //    }

    //    public ShadowMap(Device device, Settings settings)
    //    {
    //        if (device == null) throw new ArgumentNullException("device");
    //        if (settings == null) throw new ArgumentNullException("settings");

    //        Device = device;
    //        this.settings = settings;
            
    //        shadowMapEffect = new ShadowMapEffect(device);
    //        switch (settings.Technique)
    //        {
    //            case Techniques.Vsm:
    //                shadowMapEffect.Form = ShadowMapEffectForm.Variance;
    //                break;
    //            default:
    //                shadowMapEffect.Form = ShadowMapEffectForm.Basic;
    //                break;
    //        }

    //        SplitCount = settings.SplitCount;

    //        splitDistances = new float[SplitCount + 1];
    //        safeSplitDistances = new float[SplitCount + 1];
    //        safeSplitLightViewProjections = new Matrix[SplitCount];
    //        safeSplitShadowMaps = new Texture2D[SplitCount];

    //        splitLightCameras = new LightCamera[SplitCount];
    //        for (int i = 0; i < splitLightCameras.Length; i++)
    //            splitLightCameras[i] = new LightCamera();

    //        // TODO: 初期容量。
    //        splitShadowCasters = new Queue<ShadowCaster>[SplitCount];
    //        for (int i = 0; i < splitShadowCasters.Length; i++)
    //            splitShadowCasters[i] = new Queue<ShadowCaster>();

    //        view = Matrix.Identity;
    //        projection = Matrix.Identity;
    //        frustum = new BoundingFrustum(Matrix.Identity);
            
    //        splitFrustums = new BoundingFrustum[SplitCount];
    //        for (int i = 0; i < splitFrustums.Length; i++)
    //            splitFrustums[i] = new BoundingFrustum(Matrix.Identity);

    //        dirtyFlags |= DirtyFlags.RenderTargets | DirtyFlags.GaussianBlur;
    //    }

    //    public void Prepare()
    //    {
    //        UpdateRenderTargets();
    //        UpdateGaussianBlur();
    //        UpdateFrustum();

    //        // 視錐台を含む AABB をシーン領域のデフォルトとする。
    //        PrepareSplitFrustums(ref frustumBox);
    //    }

    //    public void Prepare(ref BoundingBox sceneBox)
    //    {
    //        UpdateRenderTargets();
    //        UpdateGaussianBlur();
    //        UpdateFrustum();

    //        PrepareSplitFrustums(ref sceneBox);
    //    }

    //    void UpdateRenderTargets()
    //    {
    //        if ((dirtyFlags & DirtyFlags.RenderTargets) != 0)
    //        {
    //            if (splitRenderTargets != null)
    //            {
    //                for (int i = 0; i < splitRenderTargets.Length; i++)
    //                    splitRenderTargets[i].Dispose();
    //            }
    //            else
    //            {
    //                splitRenderTargets = new RenderTarget[SplitCount];
    //            }

    //            var format = SurfaceFormat.Single;
    //            if (technique == Techniques.Vsm)
    //                format = SurfaceFormat.Vector2;

    //            // ブラーをかける場合があるので RenderTargetUsage.Preserve。
    //            for (int i = 0; i < splitRenderTargets.Length; i++)
    //            {
    //                splitRenderTargets[i] = Device.CreateRenderTarget();
    //                splitRenderTargets[i].Width = size;
    //                splitRenderTargets[i].Height = size;
    //                splitRenderTargets[i].Format = format;
    //                splitRenderTargets[i].RenderTargetUsage = RenderTargetUsage.Preserve;
    //                splitRenderTargets[i].Name = "ShadowMap" + i;
    //                splitRenderTargets[i].Initialize();
    //            }

    //            dirtyFlags &= ~DirtyFlags.RenderTargets;
    //        }
    //    }

    //    void UpdateGaussianBlur()
    //    {
    //        if (technique != Techniques.Vsm)
    //            return;

    //        if ((dirtyFlags & DirtyFlags.GaussianBlur) != 0)
    //        {
    //            if (blur != null)
    //            {
    //                blur.Dispose();
    //            }

    //            blur = new GaussianBlur(Device, size, size, SurfaceFormat.Vector2);

    //            dirtyFlags &= ~DirtyFlags.GaussianBlur;
    //        }

    //        blur.Radius = BlurRadius;
    //        blur.Amount = BlurAmount;
    //    }

    //    void UpdateFrustum()
    //    {
    //        if ((dirtyFlags & DirtyFlags.Projection) != 0)
    //        {
    //            Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlaneDistance, farPlaneDistance, out projection);

    //            dirtyFlags &= ~DirtyFlags.Projection;
    //        }

    //        Matrix viewProjection;
    //        Matrix.Multiply(ref view, ref projection, out viewProjection);

    //        frustum.Matrix = viewProjection;

    //        frustum.GetCorners(corners);
    //        BoundingBox.CreateFromPoints(corners, out frustumBox);
    //    }

    //    void PrepareSplitFrustums(ref BoundingBox sceneBox)
    //    {
    //        var far = CalculateFarPlaneDistance(ref sceneBox);

    //        CalculateSplitDistances(far);

    //        for (int i = 0; i < SplitCount; i++)
    //        {
    //            var splitNear = splitDistances[i];
    //            var splitFar = splitDistances[i + 1];

    //            Matrix projection;
    //            Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, splitNear, splitFar, out projection);

    //            Matrix viewProjection;
    //            Matrix.Multiply(ref view, ref projection, out viewProjection);

    //            splitFrustums[i].Matrix = viewProjection;
    //        }
    //    }

    //    public void TryAddShadowCaster(ShadowCaster shadowCaster)
    //    {
    //        if (!shadowCaster.SphereWorld.Intersects(frustumBoundingBox)) return;
    //        if (!shadowCaster.BoxWorld.Intersects(frustumBoundingBox)) return;

    //        for (int i = 0; i < splitFrustums.Length; i++)
    //        {
    //            var splitFrustum = splitFrustums[i];

    //            bool shouldAdd = false;
    //            if (shadowCaster.SphereWorld.Intersects(splitFrustum))
    //            {
    //                shouldAdd = true;
    //            }
    //            else if (shadowCaster.BoxWorld.Intersects(splitFrustum))
    //            {
    //                shouldAdd = true;
    //            }

    //            if (shouldAdd)
    //            {
    //                // 投影オブジェクトとして登録。
    //                splitShadowCasters[i].Enqueue(shadowCaster);

    //                // AABB の頂点を包含座標として登録。
    //                // TODO:
    //                // これをやると領域が広くなりすぎてカメラが遠方に移動してしまい、
    //                // PSSM の恩恵が得られなくなる。
    //                //splitLightCameras[i].AddLightVolumePoints(corners);

    //                //break;
    //            }
    //        }
    //    }

    //    // シャドウ マップを描画。
    //    public void Draw(DeviceContext context, ref Vector3 lightDirection)
    //    {
    //        context.DepthStencilState = DepthStencilState.Default;
    //        context.BlendState = BlendState.Opaque;

    //        // 各ライト カメラで描画。
    //        for (int i = 0; i < splitFrustums.Length; i++)
    //        {
    //            var splitFrustum = splitFrustums[i];
    //            var renderTarget = splitRenderTargets[i];
    //            var shadowCasters = splitShadowCasters[i];

    //            //------------------------------------------------------------
    //            // ライトのビュー×射影行列の更新

    //            splitFrustum.GetCorners(corners);
    //            splitLightCameras[i].AddLightVolumePoints(corners);
    //            splitLightCameras[i].Update(lightDirection);

    //            //------------------------------------------------------------
    //            // エフェクト

    //            shadowMapEffect.LightViewProjection = splitLightCameras[i].LightViewProjection;

    //            //------------------------------------------------------------
    //            // 描画

    //            context.SetRenderTarget(renderTarget.GetRenderTargetView());
    //            context.Clear(Color.White);

    //            while (0 < shadowCasters.Count)
    //            {
    //                var shadowCaster = shadowCasters.Dequeue();
    //                shadowCaster.Draw(shadowMapEffect);
    //            }

    //            if (shadowMapEffect.ShadowMapTechnique == Techniques.Vsm && blur != null)
    //                blur.Filter(renderTarget);

    //            context.SetRenderTarget(null);

    //            TextureDisplay.Add(renderTarget);
    //        }
    //    }

    //    public Texture2D GetShadowMap(int index)
    //    {
    //        if (index < 0 || splitRenderTargets.Length < index) throw new ArgumentOutOfRangeException("index");

    //        return splitRenderTargets[index];
    //    }

    //    float CalculateFarPlaneDistance(ref BoundingBox sceneBox)
    //    {
    //        // 領域の最も遠い点を探す。
    //        // z = 0 は視点。
    //        // より小さな z がより遠い点。
    //        var maxFar = 0.0f;

    //        sceneBox.GetCorners(corners);
            
    //        for (int i = 0; i < 8; i++)
    //        {
    //            // ビュー座標へ変換。
    //            var z =
    //                corners[i].X * view.M13 +
    //                corners[i].Y * view.M23 +
    //                corners[i].Z * view.M33 +
    //                view.M43;

    //            if (z < maxFar) maxFar = z;
    //        }

    //        // 見つかった最も遠い点の z で farPlaneDistance を決定。
    //        return nearPlaneDistance - maxFar;
    //    }

    //    void CalculateSplitDistances(float far)
    //    {
    //        var near = nearPlaneDistance;
    //        var farNearRatio = far / near;
    //        var splitLambda = settings.SplitLambda;

    //        float inverseSplitCount = 1.0f / (float) SplitCount;

    //        for (int i = 0; i < splitDistances.Length; i++)
    //        {
    //            float idm = i * inverseSplitCount;

    //            // CL = n * (f / n)^(i / m)
    //            float log = (float) (near * Math.Pow(farNearRatio, idm));

    //            // CU = n + (f - n) * (i / m)
    //            float uniform = near + (far - near) * idm;
    //            // REFERENCE: the version (?) in some actual codes,
    //            //float uniform = (near + idm) * (far - near);

    //            // C = CL * lambda + CU * (1 - lambda)
    //            splitDistances[i] = log * splitLambda + uniform * (1.0f - splitLambda);
    //        }

    //        splitDistances[0] = near;
    //        splitDistances[splitDistances.Length - 1] = far;
    //    }

    //    #region IDisposable

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    bool disposed;

    //    ~ShadowMap()
    //    {
    //        Dispose(false);
    //    }

    //    void Dispose(bool disposing)
    //    {
    //        if (disposed) return;

    //        if (disposing)
    //        {
    //            shadowMapEffect.Dispose();
    //            if (blur != null) blur.Dispose();

    //            foreach (var splitRenderTarget in splitRenderTargets)
    //                splitRenderTarget.Dispose();
    //        }

    //        disposed = true;
    //    }

    //    #endregion
    //}
}
