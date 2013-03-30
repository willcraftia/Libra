#region Using

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

using D3DCEffectFlags = SharpDX.D3DCompiler.EffectFlags;
using D3DCInclude = SharpDX.D3DCompiler.Include;
using D3DCIncludeType = SharpDX.D3DCompiler.IncludeType;
using D3DCShaderBytecode = SharpDX.D3DCompiler.ShaderBytecode;
using D3DCShaderFlags = SharpDX.D3DCompiler.ShaderFlags;
using D3DCShaderSignature = SharpDX.D3DCompiler.ShaderSignature;

#endregion

namespace Libra.Graphics.SharpDX.Compiler
{
    public sealed class ShaderCompiler : IDisposable
    {
        #region PathCollection

        public sealed class PathCollection : Collection<string>
        {
            internal PathCollection() { }
        }

        #endregion

        #region D3DCIncludeImpl

        sealed class D3DCIncludeImpl : D3DCInclude
        {
            ShaderCompiler compiler;

            public IDisposable Shadow { get; set; }

            internal D3DCIncludeImpl(ShaderCompiler compiler)
            {
                this.compiler = compiler;
            }

            public Stream Open(D3DCIncludeType type, string fileName, Stream parentStream)
            {
                return compiler.OpenIncludeFile(type, fileName);
            }

            public void Close(Stream stream)
            {
                compiler.CloseIncludeFile(stream);
            }

            public void Dispose()
            {
                // TODO
                // これで良いの？説明が足りない。
                if (Shadow != null)
                    Shadow.Dispose();
            }
        }

        #endregion

        D3DCIncludeImpl d3dcInclude;

        string parentFilePath;

        // D3DCOMPILE Constants
        // http://msdn.microsoft.com/en-us/library/gg615083(v=vs.85).aspx

        /// <summary>
        /// デバッグ情報を出力コードへ挿入するか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// デバッグ情報には file/line/type/symbol が挿入されます。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_DEBUG
        /// </remarks>
        public bool EnableDebug { get; set; }

        /// <summary>
        /// 生成されたコードの検査を行うか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// 過去にコンパイルが成功しているシェーダに対してのみ、検査をスキップすることをお薦めします。
        /// DirectX は、デバイスへシェーダを設定する前に、常にシェーダを検査します。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_SKIP_VALIDATION
        /// </remarks>
        public bool SkipValidation { get; set; }

        /// <summary>
        /// 最適化処理をスキップするか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// デバッグ目的でのみ、最適化処理をスキップすることをお薦めします。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_SKIP_OPTIMIZATION
        /// </remarks>
        public bool SkipOptimization { get; set; }

        /// <summary>
        /// 行列を行優先 (row_major) とするか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// HLSL で明示していない場合のデフォルトは列優先 (column_major) です。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_PACK_MATRIX_ROW_MAJOR
        /// </remarks>
        public bool PackMatrixRowMajor { get; set; }

        /// <summary>
        /// 行列を列優先 (column_major) とするか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// 列優先ではドット積をベクトル×行列で処理できるため、一般的にはより効率的です。
        /// HLSL で明示していない場合のデフォルトは列優先 (column_major) です。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_PACK_MATRIX_COLUMN_MAJOR
        /// </remarks>
        public bool PackMatrixColumnMajor { get; set; }

        /// <summary>
        /// 厳密にコンパイルするか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// 厳密なコンパイルでは、古い非推奨な構文を許可しません。
        /// デフォルトでは、非推奨な構文を許可します。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_ENABLE_STRICTNESS
        /// </remarks>
        public bool EnableStrictness { get; set; }

        /// <summary>
        /// 古いシェーダを 5_0 ターゲットでコンパイルするか否かを示す値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// D3DCompiler.h: D3DCOMPILE_ENABLE_BACKWARDS_COMPATIBILITY
        /// </remarks>
        public bool EnableBackwardsCompatibility { get; set; }

        /// <summary>
        /// 最適化レベルを取得または設定します。
        /// </summary>
        /// <remarks>
        /// デフォルトは Level1 です。
        /// 
        /// D3DCompiler.h:
        /// D3DCOMPILE_OPTIMIZATION_LEVEL0
        /// D3DCOMPILE_OPTIMIZATION_LEVEL1
        /// D3DCOMPILE_OPTIMIZATION_LEVEL2
        /// D3DCOMPILE_OPTIMIZATION_LEVEL3
        /// </remarks>
        public OptimizationLevels OptimizationLevel { get; set; }

        /// <summary>
        /// コンパイル時の全ての警告をエラーとして処理します。
        /// </summary>
        /// <remarks>
        /// 新しいシェーダ コードでは、全ての警告を解決し、
        /// 発見が困難なコード上の問題を減らすために、
        /// この設定を ON にすることをお薦めします。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_WARNINGS_ARE_ERRORS
        /// </remarks>
        public bool WarningsAreErrors { get; set; }

        /// <summary>
        /// シェーダ ファイルのルート パスを取得または設定します。
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// システム インクルード (#include &lt;filename&gt 形式)
        /// の検索パスのコレクションを取得します。
        /// </summary>
        public PathCollection SystemIncludePaths { get; private set; }

        public VertexShaderProfile VertexShaderProfile { get; set; }

        public PixelShaderProfile PixelShaderProfile { get; set; }

        public ShaderCompiler()
        {
            d3dcInclude = new D3DCIncludeImpl(this);

            OptimizationLevel = OptimizationLevels.Level1;
            
            SystemIncludePaths = new PathCollection();
            VertexShaderProfile = VertexShaderProfile.vs_5_0;
            PixelShaderProfile = PixelShaderProfile.ps_5_0;
        }

        public static byte[] ParseInputSignature(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetInputSignature(shaderBytecode).Data;
        }

        public static byte[] ParseOutputSignature(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetOutputSignature(shaderBytecode).Data;
        }

        public static byte[] ParseInputOutputSignature(byte[] shaderBytecode)
        {
            return D3DCShaderSignature.GetInputOutputSignature(shaderBytecode).Data;
        }

        public byte[] CompileVertexShader(Stream stream, string entrypoint)
        {
            return Compile(stream, entrypoint, ToString(VertexShaderProfile));
        }

        public byte[] CompilePixelShader(Stream stream, string entrypoint)
        {
            return Compile(stream, entrypoint, ToString(PixelShaderProfile));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// ストリームからシェーダ コードをを読み込む場合、
        /// シェーダ ファイルのパスは不明であるため、
        /// ローカル インクルード (#include "filename" 形式) は、
        /// 絶対パス指定、あるいは、カレント ディレクトリからの相対パスのみが有効となります。
        /// </remarks>
        /// <param name="stream"></param>
        /// <param name="entrypoint"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public byte[] Compile(Stream stream, string entrypoint, string profile)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (entrypoint == null) throw new ArgumentNullException("entrypoint");
            if (profile == null) throw new ArgumentNullException("profile");

            // 注意
            //
            // シェーダ ファイルは ASCII 限定。
            // Shift-JIS ならば日本語を含める事が可能だが、
            // UTF-8 はコンパイル エラーとなる。

            string shaderSource;
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                shaderSource = reader.ReadToEnd();
            }

            var d3dcShaderFlags = ResolveD3DCShaderFlags();

            var d3dCCompilationResult = D3DCShaderBytecode.Compile(
                Encoding.ASCII.GetBytes(shaderSource), entrypoint, profile,
                d3dcShaderFlags, D3DCEffectFlags.None, null, d3dcInclude);

            return d3dCCompilationResult.Bytecode.Data;
        }

        public byte[] CompileVertexShader(string path, string entrypoint)
        {
            return CompileFromFile(path, entrypoint, ToString(VertexShaderProfile));
        }

        public byte[] CompilePixelShader(string path, string entrypoint)
        {
            return CompileFromFile(path, entrypoint, ToString(PixelShaderProfile));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// ファイルからシェーダ コードを読み込む場合、
        /// ローカル インクルード (#include "filename" 形式) は、
        /// 絶対パス指定、および、カレント ディレクトリからの相対パスに加え、
        /// 親ファイルからの相対パスも有効となります。
        /// </remarks>
        /// <param name="path"></param>
        /// <param name="entrypoint"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public byte[] CompileFromFile(string path, string entrypoint, string profile)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (entrypoint == null) throw new ArgumentNullException("entrypoint");
            if (profile == null) throw new ArgumentNullException("profile");

            string realPath;

            if (RootPath == null)
            {
                realPath = path;
            }
            else
            {
                realPath = Path.Combine(RootPath, path);
            }

            parentFilePath = realPath;

            using (var stream = File.OpenRead(realPath))
            {
                return Compile(stream, entrypoint, profile);
            }
        }

        static string ToString(VertexShaderProfile profile)
        {
            switch (profile)
            {
                case VertexShaderProfile.vs_1_1:
                    return "vs_1_1";
                case VertexShaderProfile.vs_2_0:
                    return "vs_2_0";
                case VertexShaderProfile.vs_2_a:
                    return "vs_2_a";
                case VertexShaderProfile.vs_2_sw:
                    return "vs_2_sw";
                case VertexShaderProfile.vs_3_0:
                    return "vs_3_0";
                case VertexShaderProfile.vs_3_0_sw:
                    return "vs_3_0_sw";
                case VertexShaderProfile.vs_4_0:
                    return "vs_4_0";
                case VertexShaderProfile.vs_4_0_level_9_1:
                    return "vs_4_0_level_9_1";
                case VertexShaderProfile.vs_4_0_level_9_3:
                    return "vs_4_0_level_9_3";
                case VertexShaderProfile.vs_4_1:
                    return "vs_4_1";
                case VertexShaderProfile.vs_5_0:
                    return "vs_5_0";
                default:
                    throw new InvalidOperationException();
            }
        }

        static string ToString(PixelShaderProfile profile)
        {
            switch (profile)
            {
                case PixelShaderProfile.ps_2_0:
                    return "ps_2_0";
                case PixelShaderProfile.ps_2_a:
                    return "ps_2_a";
                case PixelShaderProfile.ps_2_b:
                    return "ps_2_b";
                case PixelShaderProfile.ps_2_sw:
                    return "ps_2_sw";
                case PixelShaderProfile.ps_3_0:
                    return "ps_3_0";
                case PixelShaderProfile.ps_3_sw:
                    return "ps_3_sw";
                case PixelShaderProfile.ps_4_0:
                    return "ps_4_0";
                case PixelShaderProfile.ps_4_0_level_9_1:
                    return "ps_4_0_level_9_1";
                case PixelShaderProfile.ps_4_0_level_9_3:
                    return "ps_4_0_level_9_3";
                case PixelShaderProfile.ps_4_1:
                    return "ps_4_1";
                case PixelShaderProfile.ps_5_0:
                    return "ps_5_0";
                default:
                    throw new InvalidOperationException();
            }
        }

        Stream OpenIncludeFile(D3DCIncludeType type, string fileName)
        {
            var filePath = ResolveIncludePath(type, fileName);
            return File.OpenRead(filePath);
        }

        void CloseIncludeFile(Stream stream)
        {
            stream.Close();
        }

        string ResolveIncludePath(D3DCIncludeType type, string fileName)
        {
            if (type == D3DCIncludeType.Local)
                return ResolveLocalIncludePath(fileName);

            return ResolveSystemIncludePath(fileName);
        }

        string ResolveLocalIncludePath(string fileName)
        {
            if (File.Exists(fileName))
                return fileName;

            if (parentFilePath != null)
            {
                var basePath = Path.GetDirectoryName(parentFilePath);
                var parentRelativePath = Path.Combine(basePath, fileName);
                if (File.Exists(parentRelativePath))
                    return parentRelativePath;
            }

            throw new FileNotFoundException("Local include file not found: " + fileName);
        }

        string ResolveSystemIncludePath(string fileName)
        {
            foreach (var path in SystemIncludePaths)
            {
                var filePath = Path.Combine(path, fileName);
                if (File.Exists(filePath))
                    return filePath;
            }

            throw new FileNotFoundException("System include file not found: " + fileName);
        }

        D3DCShaderFlags ResolveD3DCShaderFlags()
        {
            var flags = D3DCShaderFlags.None;

            if (EnableDebug)
                flags |= D3DCShaderFlags.Debug;

            if (SkipValidation)
                flags |= D3DCShaderFlags.SkipValidation;

            if (SkipOptimization)
                flags |= D3DCShaderFlags.SkipOptimization;

            if (PackMatrixRowMajor)
                flags |= D3DCShaderFlags.PackMatrixRowMajor;

            if (PackMatrixColumnMajor)
                flags |= D3DCShaderFlags.PackMatrixColumnMajor;

            if (EnableStrictness)
                flags |= D3DCShaderFlags.EnableStrictness;

            if (EnableBackwardsCompatibility)
                flags |= D3DCShaderFlags.EnableBackwardsCompatibility;

            switch (OptimizationLevel)
            {
                case OptimizationLevels.Level0:
                    flags |= D3DCShaderFlags.OptimizationLevel0;
                    break;
                case OptimizationLevels.Level1:
                    flags |= D3DCShaderFlags.OptimizationLevel1;
                    break;
                case OptimizationLevels.Level2:
                    flags |= D3DCShaderFlags.OptimizationLevel2;
                    break;
                case OptimizationLevels.Level3:
                    flags |= D3DCShaderFlags.OptimizationLevel3;
                    break;
            }

            if (WarningsAreErrors)
                flags |= D3DCShaderFlags.WarningsAreErrors;

            return flags;
        }

        #region IDisposable

        bool disposed;

        ~ShaderCompiler()
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
                d3dcInclude.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
