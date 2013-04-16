#region Using

using System;

#endregion

// SharpDX.Collision を移植。

namespace Libra
{
    public static class Collision
    {
        public static bool RayIntersectsPoint(ref Ray ray, ref Vector3 point)
        {
            //Source: RayIntersectsSphere
            //Reference: None

            Vector3 m;
            Vector3.Subtract(ref ray.Position, ref point, out m);

            //Same thing as RayIntersectsSphere except that the radius of the sphere (point)
            //is the epsilon for zero.
            float b = Vector3.Dot(m, ray.Direction);
            float c = Vector3.Dot(m, m) - MathHelper.ZeroTolerance;

            if (c > 0f && b > 0f)
                return false;

            float discriminant = b * b - c;

            if (discriminant < 0f)
                return false;

            return true;
        }

        public static bool RayIntersectsRay(ref Ray ray1, ref Ray ray2, out Vector3 point)
        {
            //Source: Real-Time Rendering, Third Edition
            //Reference: Page 780

            Vector3 cross;

            Vector3.Cross(ref ray1.Direction, ref ray2.Direction, out cross);
            float denominator = cross.Length();

            //Lines are parallel.
            if (Math.Abs(denominator) < MathHelper.ZeroTolerance)
            {
                //Lines are parallel and on top of each other.
                if (Math.Abs(ray2.Position.X - ray1.Position.X) < MathHelper.ZeroTolerance &&
                    Math.Abs(ray2.Position.Y - ray1.Position.Y) < MathHelper.ZeroTolerance &&
                    Math.Abs(ray2.Position.Z - ray1.Position.Z) < MathHelper.ZeroTolerance)
                {
                    point = Vector3.Zero;
                    return true;
                }
            }

            denominator = denominator * denominator;

            //3x3 matrix for the first ray.
            float m11 = ray2.Position.X - ray1.Position.X;
            float m12 = ray2.Position.Y - ray1.Position.Y;
            float m13 = ray2.Position.Z - ray1.Position.Z;
            float m21 = ray2.Direction.X;
            float m22 = ray2.Direction.Y;
            float m23 = ray2.Direction.Z;
            float m31 = cross.X;
            float m32 = cross.Y;
            float m33 = cross.Z;

            //Determinant of first matrix.
            float dets =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //3x3 matrix for the second ray.
            m21 = ray1.Direction.X;
            m22 = ray1.Direction.Y;
            m23 = ray1.Direction.Z;

            //Determinant of the second matrix.
            float dett =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //t values of the point of intersection.
            float s = dets / denominator;
            float t = dett / denominator;

            //The points of intersection.
            Vector3 point1 = ray1.Position + (s * ray1.Direction);
            Vector3 point2 = ray2.Position + (t * ray2.Direction);

            //If the points are not equal, no intersection has occured.
            if (Math.Abs(point2.X - point1.X) > MathHelper.ZeroTolerance ||
                Math.Abs(point2.Y - point1.Y) > MathHelper.ZeroTolerance ||
                Math.Abs(point2.Z - point1.Z) > MathHelper.ZeroTolerance)
            {
                point = Vector3.Zero;
                return false;
            }

            point = point1;
            return true;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            float direction;
            Vector3.Dot(ref plane.Normal, ref ray.Direction, out direction);

            if (Math.Abs(direction) < MathHelper.ZeroTolerance)
            {
                distance = 0f;
                return false;
            }

            float position;
            Vector3.Dot(ref plane.Normal, ref ray.Position, out position);
            distance = (-plane.D - position) / direction;

            if (distance < 0f)
            {
                if (distance < -MathHelper.ZeroTolerance)
                {
                    distance = 0;
                    return false;
                }

                distance = 0f;
            }

            return true;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            float distance;
            if (!RayIntersectsPlane(ref ray, ref plane, out distance))
            {
                point = Vector3.Zero;
                return false;
            }

            point = ray.Position + (ray.Direction * distance);
            return true;
        }

        public static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float distance)
        {
            //Source: Fast Min Storage Ray / Triangle Intersection
            //Reference: http://www.cs.virginia.edu/~gfx/Courses/2003/ImageSynthesis/papers/Acceleration/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf

            //Compute vectors along two edges of the triangle.
            Vector3 edge1, edge2;

            //Edge 1
            edge1.X = vertex2.X - vertex1.X;
            edge1.Y = vertex2.Y - vertex1.Y;
            edge1.Z = vertex2.Z - vertex1.Z;

            //Edge2
            edge2.X = vertex3.X - vertex1.X;
            edge2.Y = vertex3.Y - vertex1.Y;
            edge2.Z = vertex3.Z - vertex1.Z;

            //Cross product of ray direction and edge2 - first part of determinant.
            Vector3 directioncrossedge2;
            directioncrossedge2.X = (ray.Direction.Y * edge2.Z) - (ray.Direction.Z * edge2.Y);
            directioncrossedge2.Y = (ray.Direction.Z * edge2.X) - (ray.Direction.X * edge2.Z);
            directioncrossedge2.Z = (ray.Direction.X * edge2.Y) - (ray.Direction.Y * edge2.X);

            //Compute the determinant.
            float determinant;
            //Dot product of edge1 and the first part of determinant.
            determinant = (edge1.X * directioncrossedge2.X) + (edge1.Y * directioncrossedge2.Y) + (edge1.Z * directioncrossedge2.Z);

            //If the ray is parallel to the triangle plane, there is no collision.
            //This also means that we are not culling, the ray may hit both the
            //back and the front of the triangle.
            if (determinant > -MathHelper.ZeroTolerance && determinant < MathHelper.ZeroTolerance)
            {
                distance = 0f;
                return false;
            }

            float inversedeterminant = 1.0f / determinant;

            //Calculate the U parameter of the intersection point.
            Vector3 distanceVector;
            distanceVector.X = ray.Position.X - vertex1.X;
            distanceVector.Y = ray.Position.Y - vertex1.Y;
            distanceVector.Z = ray.Position.Z - vertex1.Z;

            float triangleU;
            triangleU = (distanceVector.X * directioncrossedge2.X) + (distanceVector.Y * directioncrossedge2.Y) + (distanceVector.Z * directioncrossedge2.Z);
            triangleU *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleU < 0f || triangleU > 1f)
            {
                distance = 0f;
                return false;
            }

            //Calculate the V parameter of the intersection point.
            Vector3 distancecrossedge1;
            distancecrossedge1.X = (distanceVector.Y * edge1.Z) - (distanceVector.Z * edge1.Y);
            distancecrossedge1.Y = (distanceVector.Z * edge1.X) - (distanceVector.X * edge1.Z);
            distancecrossedge1.Z = (distanceVector.X * edge1.Y) - (distanceVector.Y * edge1.X);

            float triangleV;
            triangleV = ((ray.Direction.X * distancecrossedge1.X) + (ray.Direction.Y * distancecrossedge1.Y)) + (ray.Direction.Z * distancecrossedge1.Z);
            triangleV *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleV < 0f || triangleU + triangleV > 1f)
            {
                distance = 0f;
                return false;
            }

            //Compute the distance along the ray to the triangle.
            float raydistance;
            raydistance = (edge2.X * distancecrossedge1.X) + (edge2.Y * distancecrossedge1.Y) + (edge2.Z * distancecrossedge1.Z);
            raydistance *= inversedeterminant;

            //Is the triangle behind the ray origin?
            if (raydistance < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = raydistance;
            return true;
        }

        public static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 point)
        {
            float distance;
            if (!RayIntersectsTriangle(ref ray, ref vertex1, ref vertex2, ref vertex3, out distance))
            {
                point = Vector3.Zero;
                return false;
            }

            point = ray.Position + (ray.Direction * distance);
            return true;
        }

        public static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out float distance)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 179

            distance = 0f;
            float tmax = float.MaxValue;

            if (Math.Abs(ray.Direction.X) < MathHelper.ZeroTolerance)
            {
                if (ray.Position.X < box.Min.X || ray.Position.X > box.Max.X)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.Direction.X;
                float t1 = (box.Min.X - ray.Position.X) * inverse;
                float t2 = (box.Max.X - ray.Position.X) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            if (Math.Abs(ray.Direction.Y) < MathHelper.ZeroTolerance)
            {
                if (ray.Position.Y < box.Min.Y || ray.Position.Y > box.Max.Y)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.Direction.Y;
                float t1 = (box.Min.Y - ray.Position.Y) * inverse;
                float t2 = (box.Max.Y - ray.Position.Y) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            if (Math.Abs(ray.Direction.Z) < MathHelper.ZeroTolerance)
            {
                if (ray.Position.Z < box.Min.Z || ray.Position.Z > box.Max.Z)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.Direction.Z;
                float t1 = (box.Min.Z - ray.Position.Z) * inverse;
                float t2 = (box.Max.Z - ray.Position.Z) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            return true;
        }

        public static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out Vector3 point)
        {
            float distance;
            if (!RayIntersectsBox(ref ray, ref box, out distance))
            {
                point = Vector3.Zero;
                return false;
            }

            point = ray.Position + (ray.Direction * distance);
            return true;
        }

        public static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out float distance)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 177

            Vector3 m;
            Vector3.Subtract(ref ray.Position, ref sphere.Center, out m);

            float b = Vector3.Dot(m, ray.Direction);
            float c = Vector3.Dot(m, m) - (sphere.Radius * sphere.Radius);

            if (c > 0f && b > 0f)
            {
                distance = 0f;
                return false;
            }

            float discriminant = b * b - c;

            if (discriminant < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = -b - (float) Math.Sqrt(discriminant);

            if (distance < 0f)
                distance = 0f;

            return true;
        }

        public static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out Vector3 point)
        {
            float distance;
            if (!RayIntersectsSphere(ref ray, ref sphere, out distance))
            {
                point = Vector3.Zero;
                return false;
            }

            point = ray.Position + (ray.Direction * distance);
            return true;
        }
    }
}
