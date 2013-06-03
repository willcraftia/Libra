#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

// 参考: XBOX LIve indies games - Shape Rendering
// http://xbox.create.msdn.com/ja-JP/education/catalog/sample/shape_rendering

namespace Libra.Graphics.Toolkit
{
    public sealed class DebugShapeRenderer
    {
        #region Shape

        class Shape
        {
            public VertexPositionColor[] Vertices;

            public int LineCount;

            public float Lifetime;
        }

        #endregion

        const int SphereResolution = 30;

        const int SphereLineCount = (SphereResolution + 1) * 3;

        static Vector3[] unitSphere;

        List<Shape> cachedShapes = new List<Shape>();

        List<Shape> activeShapes = new List<Shape>();

        VertexPositionColor[] verts = new VertexPositionColor[64];

        DeviceContext context;

        BasicEffect effect;

        VertexBuffer vertexBuffer;

        Vector3[] corners = new Vector3[8];

        public DebugShapeRenderer(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            effect = new BasicEffect(context.Device);
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;
            effect.DiffuseColor = Vector3.One;
            effect.World = Matrix.Identity;

            InitializeUnitSphere();
        }

        [Conditional("DEBUG")]
        public void AddLine(Vector3 a, Vector3 b, Color color)
        {
            AddLine(a, b, color, 0f);
        }

        [Conditional("DEBUG")]
        public void AddLine(Vector3 a, Vector3 b, Color color, float life)
        {
            var shape = GetShapeForLines(1, life);

            shape.Vertices[0] = new VertexPositionColor(a, color);
            shape.Vertices[1] = new VertexPositionColor(b, color);
        }

        [Conditional("DEBUG")]
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            AddTriangle(a, b, c, color, 0f);
        }

        [Conditional("DEBUG")]
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, float life)
        {
            var shape = GetShapeForLines(3, life);

            shape.Vertices[0] = new VertexPositionColor(a, color);
            shape.Vertices[1] = new VertexPositionColor(b, color);
            shape.Vertices[2] = new VertexPositionColor(b, color);
            shape.Vertices[3] = new VertexPositionColor(c, color);
            shape.Vertices[4] = new VertexPositionColor(c, color);
            shape.Vertices[5] = new VertexPositionColor(a, color);
        }

        [Conditional("DEBUG")]
        public void AddBoundingFrustum(BoundingFrustum frustum, Color color)
        {
            AddBoundingFrustum(frustum, color, 0f);
        }

        [Conditional("DEBUG")]
        public void AddBoundingFrustum(BoundingFrustum frustum, Color color, float life)
        {
            frustum.GetCorners(corners);

            AddBox(corners, color, life);
        }

        [Conditional("DEBUG")]
        public void AddBoundingBox(BoundingBox box, Color color)
        {
            AddBoundingBox(box, color, 0f);
        }

        [Conditional("DEBUG")]
        public void AddBoundingBox(BoundingBox box, Color color, float life)
        {
            box.GetCorners(corners);

            AddBox(corners, color, life);
        }

        void AddBox(Vector3[] corners, Color color, float life)
        {
            var shape = GetShapeForLines(12, life);

            // 底
            shape.Vertices[0] = new VertexPositionColor(corners[0], color);
            shape.Vertices[1] = new VertexPositionColor(corners[1], color);
            shape.Vertices[2] = new VertexPositionColor(corners[1], color);
            shape.Vertices[3] = new VertexPositionColor(corners[2], color);
            shape.Vertices[4] = new VertexPositionColor(corners[2], color);
            shape.Vertices[5] = new VertexPositionColor(corners[3], color);
            shape.Vertices[6] = new VertexPositionColor(corners[3], color);
            shape.Vertices[7] = new VertexPositionColor(corners[0], color);

            // 上面
            shape.Vertices[8] = new VertexPositionColor(corners[4], color);
            shape.Vertices[9] = new VertexPositionColor(corners[5], color);
            shape.Vertices[10] = new VertexPositionColor(corners[5], color);
            shape.Vertices[11] = new VertexPositionColor(corners[6], color);
            shape.Vertices[12] = new VertexPositionColor(corners[6], color);
            shape.Vertices[13] = new VertexPositionColor(corners[7], color);
            shape.Vertices[14] = new VertexPositionColor(corners[7], color);
            shape.Vertices[15] = new VertexPositionColor(corners[4], color);

            // 垂直方向側面
            shape.Vertices[16] = new VertexPositionColor(corners[0], color);
            shape.Vertices[17] = new VertexPositionColor(corners[4], color);
            shape.Vertices[18] = new VertexPositionColor(corners[1], color);
            shape.Vertices[19] = new VertexPositionColor(corners[5], color);
            shape.Vertices[20] = new VertexPositionColor(corners[2], color);
            shape.Vertices[21] = new VertexPositionColor(corners[6], color);
            shape.Vertices[22] = new VertexPositionColor(corners[3], color);
            shape.Vertices[23] = new VertexPositionColor(corners[7], color);
        }

        [Conditional("DEBUG")]
        public void AddBoundingSphere(BoundingSphere sphere, Color color)
        {
            AddBoundingSphere(sphere, color, 0f);
        }

        [Conditional("DEBUG")]
        public void AddBoundingSphere(BoundingSphere sphere, Color color, float life)
        {
            var shape = GetShapeForLines(SphereLineCount, life);

            for (int i = 0; i < unitSphere.Length; i++)
            {
                Vector3 vertPos = unitSphere[i] * sphere.Radius + sphere.Center;

                shape.Vertices[i] = new VertexPositionColor(vertPos, color);
            }
        }

        [Conditional("DEBUG")]
        public void Draw(float elapsedTime, Matrix view, Matrix projection)
        {
            effect.View = view;
            effect.Projection = projection;

            int vertexCount = 0;
            foreach (var shape in activeShapes)
                vertexCount += shape.LineCount * 2;

            if (0 < vertexCount)
            {
                if (verts.Length < vertexCount)
                {
                    verts = new VertexPositionColor[vertexCount * 2];

                    // 頂点配列の拡張があった場合は、頂点バッファの拡張も試行。
                    // ただし、容量は ushort.MaxValue を超えないものとする。
                    int vertexBufferSize = Math.Min(verts.Length, ushort.MaxValue);
                    if (vertexBuffer != null && vertexBuffer.VertexCount < vertexBufferSize)
                    {
                        vertexBuffer.Dispose();
                        vertexBuffer = null;
                    }

                    if (vertexBuffer == null)
                    {
                        vertexBuffer = context.Device.CreateVertexBuffer();
                        vertexBuffer.Usage = ResourceUsage.Dynamic;
                        vertexBuffer.Initialize(VertexPositionColor.VertexDeclaration, vertexBufferSize);
                    }
                }

                int lineCount = 0;
                int vertIndex = 0;
                foreach (Shape shape in activeShapes)
                {
                    lineCount += shape.LineCount;
                    int shapeVerts = shape.LineCount * 2;
                    for (int i = 0; i < shapeVerts; i++)
                        verts[vertIndex++] = shape.Vertices[i];
                }

                effect.Apply(context);

                int vertexOffset = 0;
                while (0 < lineCount)
                {
                    // 一度に描画できる最大数は頂点バッファの容量に等しい。
                    int linesToDraw = Math.Min(lineCount, vertexBuffer.VertexCount);

                    int vertexCountToDraw = linesToDraw * 2;

                    vertexBuffer.SetData(context, verts, vertexOffset, vertexCountToDraw);
                    context.SetVertexBuffer(vertexBuffer);
                    context.PrimitiveTopology = PrimitiveTopology.LineList;

                    context.Draw(vertexCountToDraw);

                    vertexOffset += vertexCountToDraw;

                    lineCount -= linesToDraw;
                }
            }

            bool resort = false;
            for (int i = activeShapes.Count - 1; i >= 0; i--)
            {
                var s = activeShapes[i];
                s.Lifetime -= elapsedTime;
                if (s.Lifetime <= 0)
                {
                    cachedShapes.Add(s);
                    activeShapes.RemoveAt(i);
                    resort = true;
                }
            }

            if (resort)
                cachedShapes.Sort(CachedShapesSort);
        }

        void InitializeUnitSphere()
        {
            unitSphere = new Vector3[SphereLineCount * 2];

            float step = MathHelper.TwoPi / SphereResolution;

            int index = 0;

            // XY
            for (float a = 0f; a < MathHelper.TwoPi; a += step)
            {
                unitSphere[index++] = new Vector3((float) Math.Cos(a), (float) Math.Sin(a), 0f);
                unitSphere[index++] = new Vector3((float) Math.Cos(a + step), (float) Math.Sin(a + step), 0f);
            }

            // XZ
            for (float a = 0f; a < MathHelper.TwoPi; a += step)
            {
                unitSphere[index++] = new Vector3((float) Math.Cos(a), 0f, (float) Math.Sin(a));
                unitSphere[index++] = new Vector3((float) Math.Cos(a + step), 0f, (float) Math.Sin(a + step));
            }

            // YZ
            for (float a = 0f; a < MathHelper.TwoPi; a += step)
            {
                unitSphere[index++] = new Vector3(0f, (float) Math.Cos(a), (float) Math.Sin(a));
                unitSphere[index++] = new Vector3(0f, (float) Math.Cos(a + step), (float) Math.Sin(a + step));
            }
        }

        int CachedShapesSort(Shape s1, Shape s2)
        {
            return s1.Vertices.Length.CompareTo(s2.Vertices.Length);
        }

        Shape GetShapeForLines(int lineCount, float life)
        {
            Shape shape = null;

            int vertCount = lineCount * 2;
            for (int i = 0; i < cachedShapes.Count; i++)
            {
                if (vertCount <= cachedShapes[i].Vertices.Length)
                {
                    shape = cachedShapes[i];
                    cachedShapes.RemoveAt(i);
                    activeShapes.Add(shape);
                    break;
                }
            }

            if (shape == null)
            {
                shape = new Shape
                {
                    Vertices = new VertexPositionColor[vertCount]
                };
                activeShapes.Add(shape);
            }

            shape.LineCount = lineCount;
            shape.Lifetime = life;

            return shape;
        }
    }
}
