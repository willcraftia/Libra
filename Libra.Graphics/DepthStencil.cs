#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class DepthStencil : Resource
    {
        bool initialized;

        int width;

        int height;

        DepthFormat format;

        int multisampleCount;

        int multisampleQuality;

        public int Width
        {
            get { return width; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                width = value;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                height = value;
            }
        }

        public DepthFormat Format
        {
            get { return format; }
            set
            {
                AssertNotInitialized();
                if (value == DepthFormat.None) throw new ArgumentException("Format must be not 'None'.", "value");

                format = value;
            }
        }

        public int MultisampleCount
        {
            get { return multisampleCount; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                multisampleCount = value;
            }
        }

        public int MultisampleQuality
        {
            get { return multisampleQuality; }
            set
            {
                AssertNotInitialized();
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                multisampleQuality = value;
            }
        }

        protected DepthStencil(IDevice device)
            : base(device)
        {
            width = 1;
            height = 1;
            format = DepthFormat.Depth24Stencil8;
            multisampleCount = 1;
            multisampleQuality = 0;
        }

        public void Initialize()
        {
            AssertNotInitialized();

            InitializeCore();

            initialized = true;
        }

        protected abstract void InitializeCore();

        void AssertNotInitialized()
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
        }
    }
}
