#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class CascadeShadowMap : IDisposable
    {
        /// <summary>
        /// 最大分割数。
        /// </summary>
        public const int MaxSplitCount = 3;

        /// <summary>
        /// デフォルト ライト カメラ ビルダ。
        /// </summary>
        static readonly BasicLightCameraBuilder DefaultLightCameraBuilder = new BasicLightCameraBuilder();

        /// <summary>
        /// PSSM 分割機能。
        /// </summary>
        PSSM pssm = new PSSM();

        /// <summary>
        /// ライト カメラ ビルダ。
        /// </summary>
        LightCameraBuilder lightCameraBuilder = DefaultLightCameraBuilder;

        /// <summary>
        /// 分割された距離の配列。
        /// </summary>
        float[] splitDistances = new float[MaxSplitCount + 1];

        /// <summary>
        /// 分割された射影行列の配列。
        /// </summary>
        Matrix[] splitProjections = new Matrix[MaxSplitCount];

        /// <summary>
        /// 分割されたシャドウ マップの配列。
        /// </summary>
        ShadowMap[] shadowMaps = new ShadowMap[MaxSplitCount];

        /// <summary>
        /// 分割されたライト カメラ空間行列の配列。
        /// </summary>
        Matrix[] lightViewProjections = new Matrix[MaxSplitCount];

        /// <summary>
        /// シャドウ マップ形式。
        /// </summary>
        ShadowMapForm shadowMapForm = ShadowMapForm.Basic;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        Vector3 lightDirection = Vector3.Down;

        /// <summary>
        /// シャドウ マップ サイズ。
        /// </summary>
        int shadowMapSize = 1024;

        /// <summary>
        /// VSM 用ガウシアン フィルタ。
        /// </summary>
        GaussianFilterSuite vsmGaussianFilterSuite;

        /// <summary>
        /// デバイス コンテキストを取得します。
        /// </summary>
        public DeviceContext DeviceContext { get; private set; }

        /// <summary>
        /// 分割数を取得または設定します。
        /// </summary>
        public int SplitCount
        {
            get { return pssm.Count; }
            set
            {
                if (value < 1 || MaxSplitCount < value) throw new ArgumentOutOfRangeException("value");

                pssm.Count = value;
            }
        }

        /// <summary>
        /// 分割ラムダ値を取得または設定します。
        /// </summary>
        public float SplitLambda
        {
            get { return pssm.Lambda; }
            set { pssm.Lambda = value; }
        }

        /// <summary>
        /// ビュー行列を取得または設定します。
        /// </summary>
        public Matrix View
        {
            get { return pssm.View; }
            set { pssm.View = value; }
        }

        /// <summary>
        /// 視野角を取得または設定します。
        /// </summary>
        public float Fov
        {
            get { return pssm.Fov; }
            set { pssm.Fov = value; }
        }

        /// <summary>
        /// アスペクト比を取得または設定します。
        /// </summary>
        public float AspectRatio
        {
            get { return pssm.AspectRatio; }
            set { pssm.AspectRatio = value; }
        }

        /// <summary>
        /// 近クリップ面距離を取得または設定します。
        /// </summary>
        public float NearClipDistance
        {
            get { return pssm.NearClipDistance; }
            set { pssm.NearClipDistance = value; }
        }

        /// <summary>
        /// 遠クリップ面距離を取得または設定します。
        /// </summary>
        public float FarClipDistance
        {
            get { return pssm.FarClipDistance; }
            set { pssm.FarClipDistance = value; }
        }

        /// <summary>
        /// シーン領域を取得または設定します。
        /// </summary>
        public BoundingBox SceneBox
        {
            get { return pssm.SceneBox; }
            set { pssm.SceneBox = value; }
        }

        /// <summary>
        /// ライト カメラ ビルダを取得または設定します。
        /// </summary>
        /// <remarks>
        /// null を設定した場合、デフォルトのライト カメラが暗黙的に設定されます。
        /// </remarks>
        public LightCameraBuilder LightCameraBuilder
        {
            get { return lightCameraBuilder; }
            set { lightCameraBuilder = value; }
        }

        /// <summary>
        /// シャドウ マップ形式を取得または設定します。
        /// </summary>
        public ShadowMapForm ShadowMapForm
        {
            get { return shadowMapForm; }
            set { shadowMapForm = value; }
        }

        /// <summary>
        /// ライトの進行方向を取得または設定します。
        /// </summary>
        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set
            {
                lightDirection = value;

                if (!lightDirection.IsZero())
                    lightDirection.Normalize();
            }
        }

        /// <summary>
        /// シャドウ マップ サイズを取得または設定します。
        /// </summary>
        public int ShadowMapSize
        {
            get { return shadowMapSize; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                shadowMapSize = value;
            }
        }

        /// <summary>
        /// 投影オブジェクト描画コールバックを取得または設定します。
        /// </summary>
        public DrawShadowCastersCallback DrawShadowCastersCallback { get; set; }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="deviceContext">デバイス コンテキスト。</param>
        public CascadeShadowMap(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;
        }

        /// <summary>
        /// シャドウ マップを描画します。
        /// </summary>
        public void Draw()
        {
            // 表示カメラの分割。
            pssm.Split(splitDistances, splitProjections);

            var currentLightCameraBuilder = lightCameraBuilder ?? DefaultLightCameraBuilder;

            // 各分割で共通のビルダ プロパティを設定。
            currentLightCameraBuilder.EyeView = pssm.View;
            currentLightCameraBuilder.LightDirection = lightDirection;
            currentLightCameraBuilder.SceneBox = pssm.SceneBox;

            for (int i = 0; i < pssm.Count; i++)
            {
                // 射影行列は分割毎に異なる。
                currentLightCameraBuilder.EyeProjection = splitProjections[i];

                // ライトのビューおよび射影行列の算出。
                Matrix lightView;
                Matrix lightProjection;
                currentLightCameraBuilder.Build(out lightView, out lightProjection);

                // 後のモデル描画用にライト空間行列を算出。
                Matrix.Multiply(ref lightView, ref lightProjection, out lightViewProjections[i]);

                // シャドウ マップの準備。
                var currentShadowMap = shadowMaps[i];
                if (currentShadowMap == null)
                {
                    currentShadowMap = new ShadowMap(DeviceContext);
                    shadowMaps[i] = currentShadowMap;
                }
                
                currentShadowMap.Form = shadowMapForm;
                currentShadowMap.Size = shadowMapSize;
                currentShadowMap.View = lightView;
                currentShadowMap.Projection = lightProjection;
                currentShadowMap.DrawShadowCastersCallback = DrawShadowCastersCallback;
                
                // シャドウ マップの描画。
                currentShadowMap.Draw();

                // VSM の場合は生成したシャドウ マップへブラーを適用。
                if (shadowMapForm == ShadowMapForm.Variance)
                {
                    if (vsmGaussianFilterSuite == null)
                    {
                        vsmGaussianFilterSuite = new GaussianFilterSuite(
                            DeviceContext, shadowMapSize, shadowMapSize, SurfaceFormat.Vector2);
                        
                        // TODO
                        vsmGaussianFilterSuite.Radius = 3;
                        vsmGaussianFilterSuite.Sigma = 1;
                    }

                    vsmGaussianFilterSuite.Filter(currentShadowMap.RenderTarget, currentShadowMap.RenderTarget);
                }
            }
        }

        /// <summary>
        /// シャドウ マップを取得します。
        /// </summary>
        /// <param name="index">分割に対応するライト カメラのインデックス。</param>
        /// <returns>シャドウ マップ。</returns>
        public ShaderResourceView GetTexture(int index)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            var shadowMap = shadowMaps[index];
            if (shadowMap == null)
            {
                return null;
            }
            else
            {
                return shadowMap.RenderTarget;
            }
        }

        /// <summary>
        /// 分割距離を取得します。
        /// </summary>
        /// <param name="index">分割に対応する分割距離のインデックス。</param>
        /// <returns>分割距離。</returns>
        public float GetSplitDistance(int index)
        {
            if ((uint) MaxSplitCount + 1 < (uint) index) throw new ArgumentOutOfRangeException("index");

            return splitDistances[index];
        }

        /// <summary>
        /// ライト カメラのビュー×射影行列を取得します。
        /// </summary>
        /// <param name="index">分割に対応するライト カメラのインデックス。</param>
        /// <returns>ライト カメラのビュー×射影行列。</returns>
        public Matrix GetLightViewProjection(int index)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return lightViewProjections[index];
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~CascadeShadowMap()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                for (int i = 0; i < shadowMaps.Length; i++)
                {
                    if (shadowMaps[i] != null) shadowMaps[i].Dispose();
                }

                if (vsmGaussianFilterSuite != null) vsmGaussianFilterSuite.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
