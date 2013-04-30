#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BasicCamera : Camera
    {
        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            View = (1 << 0),
            Projection = (1 << 1)
        }

        #endregion

        Vector3 position;

        Quaternion orientation;

        float fov;

        float aspectRatio;

        float nearClipDistance;

        float farClipDistance;

        DirtyFlags dirtyFlags;

        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;

                dirtyFlags |= DirtyFlags.View;
            }
        }

        public Quaternion Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;

                dirtyFlags |= DirtyFlags.View;
            }
        }

        public float Fov
        {
            get { return fov; }
            set
            {
                if (value <= 0.0f || Math.PI <= value) throw new ArgumentOutOfRangeException("value");

                fov = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public float AspectRatio
        {
            get { return aspectRatio; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                aspectRatio = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public float NearClipDistance
        {
            get { return nearClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                nearClipDistance = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public float FarClipDistance
        {
            get { return farClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                farClipDistance = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public Vector3 Direction
        {
            get
            {
                var baseDirection = Vector3.Forward;

                Vector3 result;
                Vector3.Transform(ref baseDirection, ref orientation, out result);

                return result;
            }
            set
            {
                if (value.IsZero()) throw new ArgumentException("Direction must be not zero.", "value");

                value.Normalize();

                var start = Vector3.Forward;
                Quaternion.CreateRotationBetween(ref start, ref value, out orientation);

                dirtyFlags |= DirtyFlags.View;
            }
        }

        public Vector3 Right
        {
            get
            {
                var baseRight = Vector3.Right;

                Vector3 result;
                Vector3.Transform(ref baseRight, ref orientation, out result);

                return result;
            }
        }

        public Vector3 Up
        {
            get
            {
                var baseUp = Vector3.Up;

                Vector3 result;
                Vector3.Transform(ref baseUp, ref orientation, out result);

                return result;
            }
        }

        public BasicCamera()
        {
            position = Vector3.Zero;
            orientation = Quaternion.Identity;
            fov = MathHelper.PiOver4;
            aspectRatio = 1.0f;
            nearClipDistance = 1.0f;
            farClipDistance = 1000.0f;
        }

        public void LookAt(Vector3 target)
        {
            LookAt(ref target);
        }

        public void LookAt(ref Vector3 target)
        {
            Vector3 direction;
            Vector3.Subtract(ref target, ref position, out direction);

            Direction = direction;
        }

        public void Rotate(Vector3 axis, float angle)
        {
            Rotate(ref axis, angle);
        }

        public void Rotate(ref Vector3 axis, float angle)
        {
            Quaternion rotation;
            Quaternion.CreateFromAxisAngle(ref axis, angle, out rotation);

            rotation.Normalize();

            Quaternion newOrientation;
            Quaternion.Multiply(ref orientation, ref rotation, out newOrientation);

            orientation = newOrientation;

            dirtyFlags |= DirtyFlags.View;
        }

        public void Yaw(float angle)
        {
            var baseAxis = Vector3.UnitY;

            Vector3 axis;
            Vector3.Transform(ref baseAxis, ref orientation, out axis);

            Rotate(ref axis, angle);
        }

        public void Pitch(float angle)
        {
            var baseAxis = Vector3.UnitX;

            Vector3 axis;
            Vector3.Transform(ref baseAxis, ref orientation, out axis);

            Rotate(ref axis, angle);
        }

        public void Roll(float angle)
        {
            var baseAxis = Vector3.UnitZ;

            Vector3 axis;
            Vector3.Transform(ref baseAxis, ref orientation, out axis);

            Rotate(ref axis, angle);
        }

        public void Update()
        {
            UpdateView();
            UpdateProjection();
        }

        void UpdateView()
        {
            if ((dirtyFlags & DirtyFlags.View) != 0)
            {
                Matrix rotation;
                Matrix.CreateFromQuaternion(ref orientation, out rotation);

                Matrix transposeRotation;
                Matrix.Transpose(ref rotation, out transposeRotation);

                Vector3 translation;
                Vector3.Transform(ref position, ref transposeRotation, out translation);

                View = transposeRotation;
                View.M41 = -translation.X;
                View.M42 = -translation.Y;
                View.M43 = -translation.Z;

                dirtyFlags &= ~DirtyFlags.View;
            }
        }

        void UpdateProjection()
        {
            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearClipDistance, farClipDistance, out Projection);

                dirtyFlags &= ~DirtyFlags.Projection;
            }
        }
    }
}
