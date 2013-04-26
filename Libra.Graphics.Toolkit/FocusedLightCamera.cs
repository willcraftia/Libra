﻿#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class FocusedLightCamera : LightCamera
    {
        // y -> -z
        // z -> y
        protected static readonly Matrix NormalToLightSpace = new Matrix(
            1,  0,  0,  0,
            0,  0,  1,  0,
            0, -1,  0,  0,
            0,  0,  0,  1);

        // y -> z
        // z -> -y
        protected static readonly Matrix LightSpaceToNormal = new Matrix(
            1,  0,  0,  0,
            0,  0, -1,  0,
            0,  1,  0,  0,
            0,  0,  0,  1);

        protected ConvexBody bodyB;

        protected ConvexBody bodyLVS;

        protected List<Vector3> bodyBPoints;

        protected List<Vector3> bodyLVSPoints;

        Vector3[] corners;

        public FocusedLightCamera()
        {
            bodyB = new ConvexBody();
            bodyLVS = new ConvexBody();
            bodyBPoints = new List<Vector3>();
            bodyLVSPoints = new List<Vector3>();
            corners = new Vector3[BoundingBox.CornerCount];
        }

        protected override void Update()
        {
            // 標準的なライト空間行列の算出。
            CalculateStandardLightSpaceMatrices();

            // 凸体 B の算出。
            CalculateBodyB();

            // 凸体 B が空の場合は生成する影が無いため、
            // 先に算出した行列をそのまま利用。
            if (bodyBPoints.Count == 0)
            {
                return;
            }

            // 凸体 LVS の算出。
            CalculateBodyLVS();

            Matrix lightSpace;
            Matrix transform;

            // 軸の変換。
            transform = NormalToLightSpace;
            TransformLightProjection(ref transform);

            // ライト空間におけるカメラ方向へ変換。
            CreateCurrentLightSpace(out lightSpace);
            CreateLightLook(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // 単位立方体へ射影。
            CreateCurrentLightSpace(out lightSpace);
            CreateTransformToUnitCube(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // 軸の変換 (元へ戻す)。
            transform = LightSpaceToNormal;
            TransformLightProjection(ref transform);

            // DirectX クリッピング空間へ変換。
            Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1, out transform);
            TransformLightProjection(ref transform);
        }

        protected void CalculateStandardLightSpaceMatrices()
        {
            // 方向性光源のための行列。
            Matrix.CreateLook(ref eyePosition, ref lightDirection, ref eyeDirection, out LightView);
            LightProjection = Matrix.Identity;

            // TODO: 点光源
        }

        protected void CalculateBodyB()
        {
            bodyBPoints.Clear();

            // TODO: 点光源

            // TODO
            // 表示カメラ位置を Ogre みたいにシーン AABB へマージする？しない？

            bodyB.Define(eyeFrustum);
            bodyB.Clip(sceneBox);

            var ray = new Ray();
            ray.Direction = -lightDirection;

            for (int ip = 0; ip < bodyB.Polygons.Count; ip++)
            {
                var polygon = bodyB.Polygons[ip];

                for (int iv = 0; iv < polygon.Vertices.Count; iv++)
                {
                    var v = polygon.Vertices[iv];

                    // TODO
                    // 重複頂点を削除するか否か (接する多角形同士の頂点は重複する)。

                    bodyBPoints.Add(v);

                    Vector3 newPoint;

                    // TODO

                    // オリジナルの場合。
                    // ライトが存在する方向へレイを伸ばし、シーン AABB との交点を追加。
                    float? intersect;
                    ray.Intersects(ref sceneBox, out intersect);

                    if (intersect != null)
                    {
                        ray.GetPoint(intersect.Value, out newPoint);

                        bodyBPoints.Add(newPoint);
                    }

                    // Ogre の場合。
                    // ライトが存在する方向へレイを伸ばし、ライトの遠クリップ距離までの点を追加。
                    //ray.Position = v;
                    //ray.GetPoint(3000, out newPoint);
                    //bodyBPoints.Add(newPoint);
                }
            }
        }

        protected void CalculateBodyLVS()
        {
            bodyLVSPoints.Clear();

            // TODO: 点光源

            bodyLVS.Define(eyeFrustum);
            bodyLVS.Clip(sceneBox);

            for (int ip = 0; ip < bodyB.Polygons.Count; ip++)
            {
                var polygon = bodyLVS.Polygons[ip];

                for (int iv = 0; iv < polygon.Vertices.Count; iv++)
                {
                    var v = polygon.Vertices[iv];

                    // TODO
                    // 重複頂点を削除するか否か (接する多角形同士の頂点は重複する)。

                    bodyLVSPoints.Add(v);
                }
            }
        }

        protected void CreateCurrentLightSpace(out Matrix result)
        {
            Matrix.Multiply(ref LightView, ref LightProjection, out result);
        }

        protected void CreateLightLook(ref Matrix lightSpace, out Matrix result)
        {
            Vector3 lookPosition = Vector3.Zero;
            Vector3 lookUp = Vector3.Up;
            Vector3 lookDirection;

            GetCameraDirectionLS(ref lightSpace, out lookDirection);
            Matrix.CreateLook(ref lookPosition, ref lookDirection, ref lookUp, out result);
        }

        protected void GetNearCameraPointWS(out Vector3 result)
        {
            // 凸体 LVS から算出。

            if (bodyLVSPoints.Count == 0)
            {
                result = Vector3.Zero;
                return;
            }

            Vector3 nearWS = bodyLVSPoints[0];
            Vector3 nearES;
            Vector3.TransformCoordinate(ref nearWS, ref eyeView, out nearES);

            for (int i = 1; i < bodyLVSPoints.Count; i++)
            {
                Vector3 pointWS = bodyLVSPoints[i];
                Vector3 pointES;
                Vector3.TransformCoordinate(ref pointWS, ref eyeView, out pointES);

                if (nearES.Z < pointES.Z)
                {
                    nearES = pointES;
                    nearWS = pointWS;
                }
            }

            result = nearWS;
        }

        protected void GetCameraDirectionLS(ref Matrix lightSpace, out Vector3 result)
        {
            Vector3 e;
            Vector3 b;

            GetNearCameraPointWS(out e);
            b = e + eyeDirection;

            // ライト空間へ変換。
            Vector3 eLS;
            Vector3 bLS;
            Vector3.TransformCoordinate(ref e, ref lightSpace, out eLS);
            Vector3.TransformCoordinate(ref b, ref lightSpace, out bLS);

            // 方向。
            result = bLS - eLS;
            
            // xz 平面 (シャドウ マップ) に平行 (射影)。
            result.Y = 0.0f;
        }

        protected void CreateTransformToUnitCube(ref Matrix lightSpace, out Matrix result)
        {
            // 凸体 B を収める単位立方体。

            BoundingBox bodyBBox;
            CreateTransformedBodyBBox(ref lightSpace, out bodyBBox);

            CreateTransformToUnitCube(ref bodyBBox.Min, ref bodyBBox.Max, out result);
        }

        void CreateTransformToUnitCube(ref Vector3 min, ref Vector3 max, out Matrix result)
        {
            // 即ち glOrtho と等価。
            // http://msdn.microsoft.com/en-us/library/windows/desktop/dd373965(v=vs.85).aspx
            // ただし、右手系から左手系への変換を省くために z スケールの符号を反転。

            result = new Matrix();

            result.M11 = 2.0f / (max.X - min.X);
            result.M22 = 2.0f / (max.Y - min.Y);
            result.M33 = 2.0f / (max.Z - min.Z);
            result.M41 = -(max.X + min.X) / (max.X - min.X);
            result.M42 = -(max.Y + min.Y) / (max.Y - min.Y);
            result.M43 = -(max.Z + min.Z) / (max.Z - min.Z);
            result.M44 = 1.0f;
        }

        protected void CreateTransformedBodyBBox(ref Matrix matrix, out BoundingBox result)
        {
            result = new BoundingBox();
            for (int i = 0; i < bodyBPoints.Count; i++)
            {
                var point = bodyBPoints[i];

                Vector3 transformed;
                Vector3.TransformCoordinate(ref point, ref matrix, out transformed);

                result.Merge(ref transformed);
            }
        }

        protected void TransformLightProjection(ref Matrix matrix)
        {
            Matrix result;
            Matrix.Multiply(ref LightProjection, ref matrix, out result);

            LightProjection = result;
        }
    }
}
