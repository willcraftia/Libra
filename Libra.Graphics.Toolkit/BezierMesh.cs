#region Using

using System;

#endregion

// Xbox LIVE Indie Games - Primitives3D より移植
// http://xbox.create.msdn.com/en-US/education/catalog/sample/primitives_3d

namespace Libra.Graphics.Toolkit
{
    public abstract class BezierMesh : PrimitiveMesh
    {
        protected BezierMesh(DeviceContext context)
            : base(context)
        {
        }

        protected static int CalculateVertexCountPerPatch(int tessellation)
        {
            return (tessellation + 1) * (tessellation + 1);
        }

        protected static int CalculateIndexCountPerPatch(int tessellation)
        {
            return tessellation * tessellation * 6;
        }

        protected void CreatePatchIndices(int tessellation, bool isMirrored)
        {
            int stride = tessellation + 1;

            for (int i = 0; i < tessellation; i++)
            {
                for (int j = 0; j < tessellation; j++)
                {
                    int[] indices =
                    {
                        i * stride + j,
                        (i + 1) * stride + j,
                        (i + 1) * stride + j + 1,

                        i * stride + j,
                        (i + 1) * stride + j + 1,
                        i * stride + j + 1,
                    };

                    if (isMirrored)
                    {
                        Array.Reverse(indices);
                    }

                    foreach (int index in indices)
                    {
                        AddIndex(CurrentVertex + index);
                    }
                }
            }
        }

        protected void CreatePatchVertices(Vector3[] patch, int tessellation, bool isMirrored)
        {
            for (int i = 0; i <= tessellation; i++)
            {
                float ti = (float) i / tessellation;

                for (int j = 0; j <= tessellation; j++)
                {
                    float tj = (float) j / tessellation;

                    Vector3 p1;
                    Vector3 p2;
                    Vector3 p3;
                    Vector3 p4;
                    Bezier(ref patch[0], ref patch[1], ref patch[2], ref patch[3], ti, out p1);
                    Bezier(ref patch[4], ref patch[5], ref patch[6], ref patch[7], ti, out p2);
                    Bezier(ref patch[8], ref patch[9], ref patch[10], ref patch[11], ti, out p3);
                    Bezier(ref patch[12], ref patch[13], ref patch[14], ref patch[15], ti, out p4);

                    Vector3 position;
                    Bezier(ref p1, ref p2, ref p3, ref p4, tj, out position);

                    Vector3 q1;
                    Vector3 q2;
                    Vector3 q3;
                    Vector3 q4;
                    Bezier(ref patch[0], ref patch[4], ref patch[8], ref patch[12], tj, out q1);
                    Bezier(ref patch[1], ref patch[5], ref patch[9], ref patch[13], tj, out q2);
                    Bezier(ref patch[2], ref patch[6], ref patch[10], ref patch[14], tj, out q3);
                    Bezier(ref patch[3], ref patch[7], ref patch[11], ref patch[15], tj, out q4);

                    Vector3 tangentA;
                    Vector3 tangentB;
                    BezierTangent(ref p1, ref p2, ref p3, ref p4, tj, out tangentA);
                    BezierTangent(ref q1, ref q2, ref q3, ref q4, ti, out tangentB);

                    Vector3 normal;
                    Vector3.Cross(ref tangentA, ref tangentB, out normal);

                    if (0.0001f < normal.Length())
                    {
                        normal.Normalize();

                        if (isMirrored)
                            normal = -normal;
                    }
                    else
                    {
                        if (0 < position.Y)
                            normal = Vector3.Up;
                        else
                            normal = Vector3.Down;
                    }

                    AddVertex(position, normal);
                }
            }
        }

        static float Bezier(float p1, float p2, float p3, float p4, float t)
        {
            return p1 * (1 - t) * (1 - t) * (1 - t) +
                   p2 * 3 * t * (1 - t) * (1 - t) +
                   p3 * 3 * t * t * (1 - t) +
                   p4 * t * t * t;
        }

        static void Bezier(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, ref Vector3 p4, float t, out Vector3 result)
        {
            result.X = Bezier(p1.X, p2.X, p3.X, p4.X, t);
            result.Y = Bezier(p1.Y, p2.Y, p3.Y, p4.Y, t);
            result.Z = Bezier(p1.Z, p2.Z, p3.Z, p4.Z, t);
        }

        static float BezierTangent(float p1, float p2, float p3, float p4, float t)
        {
            return p1 * (-1 + 2 * t - t * t) +
                   p2 * (1 - 4 * t + 3 * t * t) +
                   p3 * (2 * t - 3 * t * t) +
                   p4 * (t * t);
        }

        static void BezierTangent(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, ref Vector3 p4, float t, out Vector3 result)
        {
            result.X = BezierTangent(p1.X, p2.X, p3.X, p4.X, t);
            result.Y = BezierTangent(p1.Y, p2.Y, p3.Y, p4.Y, t);
            result.Z = BezierTangent(p1.Z, p2.Z, p3.Z, p4.Z, t);
            result.Normalize();
        }
    }
}
