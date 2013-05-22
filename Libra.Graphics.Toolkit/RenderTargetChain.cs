#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class RenderTargetChain : IDisposable
    {
        Device device;

        int width;

        int height;

        SurfaceFormat format;

        int multisampleCount;

        DepthFormat depthFormat;

        RenderTargetUsage renderTargetUsage;

        RenderTarget[] renderTargets;

        int currentIndex;

        bool renderTargetsInvalid;

        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;

                width = value;
                renderTargetsInvalid = true;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (height == value) return;

                height = value;
                renderTargetsInvalid = true;
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                if (format == value) return;

                format = value;
                renderTargetsInvalid = true;
            }
        }

        public int MultisampleCount
        {
            get { return multisampleCount; }
            set
            {
                if (multisampleCount == value) return;

                multisampleCount = value;
                renderTargetsInvalid = true;
            }
        }

        public DepthFormat DepthFormat
        {
            get { return depthFormat; }
            set
            {
                if (depthFormat == value) return;

                depthFormat = value;
                renderTargetsInvalid = true;
            }
        }

        public RenderTargetUsage RenderTargetUsage
        {
            get { return renderTargetUsage; }
            set
            {
                if (renderTargetUsage == value) return;

                renderTargetUsage = value;

                for (int i = 0; i < renderTargets.Length; i++)
                {
                    if (renderTargets[i] != null)
                        renderTargets[i].RenderTargetUsage = renderTargetUsage;
                }
            }
        }

        public RenderTarget Current
        {
            get
            {
                if (renderTargetsInvalid)
                {
                    ReleaseRenderTargets();
                    renderTargetsInvalid = false;
                }

                var current = renderTargets[currentIndex];

                if (current == null)
                {
                    current = device.CreateRenderTarget();
                    current.Width = width;
                    current.Height = height;
                    current.Format = format;
                    current.MultisampleCount = multisampleCount;
                    current.DepthFormat = depthFormat;
                    current.RenderTargetUsage = renderTargetUsage;
                    current.Initialize();

                    renderTargets[currentIndex] = current;
                }

                return current;
            }
        }

        public RenderTargetChain(Device device, int renderTargetCount = 2)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (renderTargetCount < 2) throw new ArgumentOutOfRangeException("renderTargetCount");

            this.device = device;

            renderTargets = new RenderTarget[renderTargetCount];

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;
            multisampleCount = 1;

            currentIndex = 0;
            renderTargetsInvalid = false;
        }

        public void Next()
        {
            currentIndex++;
            if (renderTargets.Length == currentIndex)
            {
                currentIndex = 0;
            }
        }

        void ReleaseRenderTargets()
        {
            for (int i = 0; i < renderTargets.Length; i++)
            {
                if (renderTargets[i] != null)
                {
                    renderTargets[i].Dispose();
                    renderTargets[i] = null;
                }
            }
        }

        #region IDisposable

        bool disposed;

        ~RenderTargetChain()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                ReleaseRenderTargets();
            }

            disposed = true;
        }

        #endregion
    }
}
