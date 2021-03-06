﻿#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ライト カメラを簡易に構築するクラスです。
    /// </summary>
    /// <remarks>
    /// このクラスは、表示カメラの視錐台を元にシーン領域を決定し、
    /// ライト空間行列の算出に利用します。
    /// </remarks>
    public sealed class BasicLightCameraBuilder : LightCameraBuilder
    {
        Vector3[] eyeFrustumCorners;

        /// <summary>
        /// ライトのある方向へシーン領域を押し出す距離を取得または設定します。
        /// </summary>
        /// <remarks>
        /// ライトのある方向へシーン領域を押し出すことで、
        /// 視錐台の外に位置する投影オブジェクトをシーン領域へ含める事ができます。
        /// </remarks>
        public float SceneExtrudeDistance { get; set; }

        public BasicLightCameraBuilder()
        {
            eyeFrustumCorners = new Vector3[BoundingFrustum.CornerCount];

            SceneExtrudeDistance = 500.0f;
        }

        protected override void BuildCore(out Matrix lightView, out Matrix lightProjection)
        {
            // ライトの仮位置と仮 UP ベクトル。
            var position = Vector3.Zero;
            var up = Vector3.Up;

            // ライト方向と仮 UP ベクトルが並行な場合、他の軸を利用。
            float dot;
            Vector3.Dot(ref up, ref lightDirection, out dot);
            if ((1 - MathHelper.FastAbs(dot) < MathHelper.ZeroTolerance))
            {
                up = Vector3.Forward;
            }

            // 仮ライト ビュー行列。
            Matrix.CreateLook(ref position, ref lightDirection, ref up, out lightView);

            // 仮ライト ビュー空間における表示カメラの境界錐台を包む境界ボックス。
            var boxLV = BoundingBox.Empty;
            eyeFrustum.GetCorners(eyeFrustumCorners);
            for (int i = 0; i < eyeFrustumCorners.Length; i++)
            {
                Vector3 cornerLV;
                Vector3.Transform(ref eyeFrustumCorners[i], ref lightView, out cornerLV);

                boxLV.Merge(ref cornerLV);
            }

            // 境界ボックスのサイズを算出。
            Vector3 boxSizeLV;
            boxLV.GetSize(out boxSizeLV);

            Vector3 halfBoxSizeLV;
            boxLV.GetHalfSize(out halfBoxSizeLV);

            // 境界ボックスの近平面の中心へライト カメラの位置を合わせる。
            var positionLV = new Vector3(
                boxLV.Min.X + halfBoxSizeLV.X,
                boxLV.Min.Y + halfBoxSizeLV.Y,
                boxLV.Max.Z);

            // 仮ビュー行列の逆行列。
            Matrix invLightView;
            Matrix.Invert(ref lightView, out invLightView);

            // 仮ビュー行列の逆行列を掛ける事で仮ビュー空間におけるライト カメラ位置をワールド空間へ。
            Vector3.Transform(ref positionLV, ref invLightView, out position);

            // 決定したライト カメラ位置によりライトのビュー行列を決定。
            Matrix.CreateLook(ref position, ref lightDirection, ref up, out lightView);

            // ビュー空間における表示カメラの境界錐台を包む境界ボックス。
            // ここで作成する境界ボックスは、SceneExtrudeDistance に応じてカメラ側へ押し出す。
            // これにより、表示カメラの境界錐台の外に存在する投影オブジェクトを
            // ライト カメラ内へ含める事ができる。
            // この処理は、ライト カメラの位置の後退では満たせず、
            // 射影空間の押し出しでなければならない点に注意する。
            boxLV = BoundingBox.Empty;
            for (int i = 0; i < eyeFrustumCorners.Length; i++)
            {
                // ビュー空間へ頂点を変換してマージ。
                Vector3 cornerLV;
                Vector3.Transform(ref eyeFrustumCorners[i], ref lightView, out cornerLV);

                boxLV.Merge(ref cornerLV);

                // ライト カメラ側へ押し出した頂点をマージ。
                cornerLV.Z += SceneExtrudeDistance;

                boxLV.Merge(ref cornerLV);
            }

            // 境界ボックスのある範囲で正射影。
            Matrix.CreateOrthographicOffCenter(
                boxLV.Min.X, boxLV.Max.X,
                boxLV.Min.Y, boxLV.Max.Y,
                -boxLV.Max.Z, -boxLV.Min.Z,
                out lightProjection);
        }
    }
}
