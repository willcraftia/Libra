#region Using

using System;
using System.IO;

using D3DCEffectFlags = SharpDX.D3DCompiler.EffectFlags;
using D3DCInclude = SharpDX.D3DCompiler.Include;
using D3DCIncludeType = SharpDX.D3DCompiler.IncludeType;
using D3DCShaderBytecode = SharpDX.D3DCompiler.ShaderBytecode;
using D3DCShaderFlags = SharpDX.D3DCompiler.ShaderFlags;
using D3DCShaderSignature = SharpDX.D3DCompiler.ShaderSignature;

#endregion

namespace Libra.Graphics.Compiler.SharpDX
{
    public sealed class SdxShaderCompiler : ShaderCompiler
    {
        #region D3DCIncludeImpl

        sealed class D3DCIncludeImpl : D3DCInclude
        {
            SdxShaderCompiler compiler;

            public IDisposable Shadow { get; set; }

            internal D3DCIncludeImpl(SdxShaderCompiler compiler)
            {
                this.compiler = compiler;
            }

            public Stream Open(D3DCIncludeType type, string fileName, Stream parentStream)
            {
                return compiler.OpenIncludeFile((IncludeType) type, fileName);
            }

            public void Close(Stream stream)
            {
                compiler.CloseIncludeFile(stream);
            }

            public void Dispose()
            {
                if (Shadow != null)
                    Shadow.Dispose();
            }
        }

        #endregion

        D3DCIncludeImpl d3dcInclude;

        public SdxShaderCompiler()
        {
            d3dcInclude = new D3DCIncludeImpl(this);
        }

        protected override byte[] CompileCore(byte[] sourceCode, string entrypoint, string profile, CompileFlags flags)
        {
            var result = D3DCShaderBytecode.Compile(
                sourceCode, entrypoint, profile, (D3DCShaderFlags) flags, D3DCEffectFlags.None, null, d3dcInclude);

            return result.Bytecode.Data;
        }

        protected override byte[] GetInputSignatureCore(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetInputSignature(shaderBytecode).Data;
        }

        protected override byte[] GetOutputSignatureCore(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetOutputSignature(shaderBytecode).Data;
        }

        protected override byte[] GetInputAndOutputSignatureCore(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetInputOutputSignature(shaderBytecode).Data;
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                d3dcInclude.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
