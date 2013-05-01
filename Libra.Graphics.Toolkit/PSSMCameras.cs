#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class PSSMCameras
    {
        public const int MaxSplitCount = 4;

        int splitCount;

        float splitLambda;

        float[] splitDistances;

        Camera[] cameras;

        BoundingFrustum frustum;

        Vector3[] corners;

        public int SplitCount
        {
            get { return splitCount; }
            set
            {
                if (value < 1 || MaxSplitCount < value) throw new ArgumentOutOfRangeException("value");

                if (splitCount == value) return;

                splitCount = value;
            }
        }

        public float SplitLambda
        {
            get { return splitLambda; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                splitLambda = value;
            }
        }

        public Camera this[int index]
        {
            get
            {
                if ((uint) splitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

                return cameras[index];
            }
        }

        public PSSMCameras()
        {
            splitDistances = new float[MaxSplitCount + 1];

            cameras = new Camera[MaxSplitCount];
            for (int i = 0; i < MaxSplitCount; i++)
            {
                cameras[i] = new Camera();
            }

            frustum = new BoundingFrustum(Matrix.Identity);

            corners = new Vector3[8];

            splitCount = 3;
            splitLambda = 0.5f;
        }

        public void Update(Matrix view, Matrix projection, BoundingBox sceneBox)
        {
            // 透視射影を前提とする。

            Matrix viewProjection;
            Matrix.Multiply(ref view, ref projection, out viewProjection);

            frustum.Matrix = viewProjection;

            // 射影行列から情報を抽出。
            float fov;
            float aspectRatio;
            float nearClipDistance;
            float farClipDistance;
            projection.DecomposePerspectiveFieldOfView(out fov, out aspectRatio, out nearClipDistance, out farClipDistance);

            // シーン領域を含みうる最小限の遠クリップ面を算出。
            float adjustedFarClipDistance = CalculateFarClipDistance(ref view, ref sceneBox, nearClipDistance);
            adjustedFarClipDistance = Math.Min(farClipDistance, adjustedFarClipDistance);

            // 分割距離を算出。
            CalculateSplitDistances(nearClipDistance, adjustedFarClipDistance);

            // 分割カメラを更新。
            for (int i = 0; i < splitCount; i++)
            {
                cameras[i].View = view;

                Matrix splitProjection;
                Matrix.CreatePerspectiveFieldOfView(
                    fov,
                    aspectRatio,
                    splitDistances[i],
                    splitDistances[i + 1],
                    out splitProjection);

                cameras[i].Projection = splitProjection;
            }
        }

        float CalculateFarClipDistance(ref Matrix view, ref BoundingBox sceneBox, float nearClipDistance)
        {
            // シーン AABB の頂点の中で最もカメラから遠い点の z 値を探す。

            float maxFarZ = 0.0f;

            sceneBox.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                // z についてのみビュー座標へ変換。
                float z =
                    corners[i].X * view.M13 +
                    corners[i].Y * view.M23 +
                    corners[i].Z * view.M33 +
                    view.M43;

                // より小さな値がより遠くの点。
                if (z < maxFarZ) maxFarZ = z;
            }

            // maxFarZ の符号を反転させて距離を算出。
            return nearClipDistance - maxFarZ;
        }

        void CalculateSplitDistances(float nearClipDistance, float farClipDistance)
        {
            if (splitCount == 1)
            {
                // 分割無しの場合における最適化。
                splitDistances[0] = nearClipDistance;
                splitDistances[1] = farClipDistance;
                return;
            }

            float n = nearClipDistance;
            float f = farClipDistance;
            float m = splitCount;
            float lambda = splitLambda;

            float fdn = f / n;
            float fsn = f - n;
            float invLambda = 1.0f - lambda;

            for (int i = 0; i < splitCount + 1; i++)
            {
                float idm = i / m;

                // CL = n * (f / n)^(i / m)
                // CU = n + (f - n) * (i / m)
                // C = CL * lambda + CU * (1 - lambda)

                float log = n * (float) Math.Pow(fdn, idm);
                float uniform = n + fsn * idm;
                splitDistances[i] = log * lambda + uniform * invLambda;
            }

            splitDistances[0] = n;
            splitDistances[splitCount + 1] = f;
        }
    }
}
