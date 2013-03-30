#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class DepthStencilState : State
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_DEFAULT_STENCIL_READ_MASK
        /// </remarks>
        public const byte DefaultStencilReadMask = 0xff;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_DEFAULT_STENCIL_WRITE_MASK
        /// </remarks>
        public const byte DefaultStencilWriteMask = 0xff;

        public static readonly DepthStencilState Default;

        public static readonly DepthStencilState DepthRead;
        
        public static readonly DepthStencilState None;

        bool depthEnable;

        bool depthWriteEnable;

        ComparisonFunction depthFunction;

        bool stencilEnable;

        byte stencilReadMask;

        byte stencilWriteMask;

        StencilOperation frontFaceStencilFail;

        StencilOperation frontFaceStencilDepthFail;

        StencilOperation frontFaceStencilPass;

        ComparisonFunction frontFaceStencilFunction;

        StencilOperation backFaceStencilFail;

        StencilOperation backFaceStencilDepthFail;

        StencilOperation backFaceStencilPass;

        ComparisonFunction backFaceStencilFunction;

        int referenceStencil;

        public bool DepthEnable
        {
            get { return depthEnable; }
            set
            {
                AssertNotFrozen();
                depthEnable = value;
            }
        }

        public bool DepthWriteEnable
        {
            get { return depthWriteEnable; }
            set
            {
                AssertNotFrozen();
                depthWriteEnable = value;
            }
        }

        public ComparisonFunction DepthFunction
        {
            get { return depthFunction; }
            set
            {
                AssertNotFrozen();
                depthFunction = value;
            }
        }

        public bool StencilEnable
        {
            get { return stencilEnable; }
            set
            {
                AssertNotFrozen();
                stencilEnable = value;
            }
        }

        public byte StencilReadMask
        {
            get { return stencilReadMask; }
            set
            {
                AssertNotFrozen();
                stencilReadMask = value;
            }
        }

        public byte StencilWriteMask
        {
            get { return stencilWriteMask; }
            set
            {
                AssertNotFrozen();
                stencilWriteMask = value;
            }
        }

        public StencilOperation FrontFaceStencilFail
        {
            get { return frontFaceStencilFail; }
            set
            {
                AssertNotFrozen();
                frontFaceStencilFail = value;
            }
        }

        public StencilOperation FrontFaceStencilDepthFail
        {
            get { return frontFaceStencilDepthFail; }
            set
            {
                AssertNotFrozen();
                frontFaceStencilDepthFail = value;
            }
        }

        public StencilOperation FrontFaceStencilPass
        {
            get { return frontFaceStencilPass; }
            set
            {
                AssertNotFrozen();
                frontFaceStencilPass = value;
            }
        }

        public ComparisonFunction FrontFaceStencilFunction
        {
            get { return frontFaceStencilFunction; }
            set
            {
                AssertNotFrozen();
                frontFaceStencilFunction = value;
            }
        }

        public StencilOperation BackFaceStencilFail
        {
            get { return backFaceStencilFail; }
            set
            {
                AssertNotFrozen();
                backFaceStencilFail = value;
            }
        }

        public StencilOperation BackFaceStencilDepthFail
        {
            get { return backFaceStencilDepthFail; }
            set
            {
                AssertNotFrozen();
                backFaceStencilDepthFail = value;
            }
        }

        public StencilOperation BackFaceStencilPass
        {
            get { return backFaceStencilPass; }
            set
            {
                AssertNotFrozen();
                backFaceStencilPass = value;
            }
        }

        public ComparisonFunction BackFaceStencilFunction
        {
            get { return backFaceStencilFunction; }
            set
            {
                AssertNotFrozen();
                backFaceStencilFunction = value;
            }
        }

        // D3D11_DEPTH_STENCIL_DESC には無い項目。
        // ID3D11DeviceContext::OMSetDepthStencilState の引数 StencilRef に相当。
        public int ReferenceStencil
        {
            get { return referenceStencil; }
            set
            {
                AssertNotFrozen();
                referenceStencil = value;
            }
        }

        static DepthStencilState()
        {
            Default = new DepthStencilState
            {
                DepthEnable = true,
                DepthWriteEnable = true,
                Name = "Default"
            };

            DepthRead = new DepthStencilState
            {
                DepthEnable = true,
                DepthWriteEnable = false,
                Name = "DepthRead"
            };

            None = new DepthStencilState
            {
                DepthEnable = false,
                DepthWriteEnable = false,
                Name = "None"
            };
        }

        public DepthStencilState()
        {
            depthEnable = true;
            depthWriteEnable = true;
            // XNA では ComparisonFunction.LessEqual。
            // ここでは D3D11 のデフォルトに従う。
            depthFunction = ComparisonFunction.Less;
            stencilEnable = false;
            stencilReadMask = DefaultStencilReadMask;
            stencilWriteMask = DefaultStencilWriteMask;
            frontFaceStencilFail = StencilOperation.Keep;
            backFaceStencilFail = StencilOperation.Keep;
            frontFaceStencilDepthFail = StencilOperation.Keep;
            backFaceStencilDepthFail = StencilOperation.Keep;
            frontFaceStencilPass = StencilOperation.Keep;
            backFaceStencilPass = StencilOperation.Keep;
            frontFaceStencilFunction = ComparisonFunction.Always;
            backFaceStencilFunction = ComparisonFunction.Always;
        }
    }
}
