#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public static class D3D11Constants
    {
        /// <summary>
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT
        /// </remarks>
        public const int IAVertexInputResourceSlotCount = 32;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// D3D11.h:  D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT。
        /// </remarks>
        public const int SimultaneousRenderTargetCount = 8;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT ( 14 )
        /// </remarks>
        public const int CommnonShaderConstantBufferApiSlotCount = 14;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT ( 16 )
        /// </remarks>
        public const int CommnonShaderSamplerSlotCount = 16;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// D3D11.h: D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT ( 128 )
        /// </remarks>
        public const int CommnonShaderInputResourceSlotCount = 16;
    }
}
