#region Using

using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

#endregion

namespace Libra.Graphics.Compiler
{
    /// <summary>
    /// シェーダをコンパイルするクラスです。
    /// </summary>
    /// <remarks>
    /// DirectX 仕様により、シェーダ ファイルの文字エンコーディングは ASCII です。
    /// シェーダ ソースコードに日本語を含める場合には、
    /// 文字エンコーディングを Shift-JIS とします。
    /// なお、UTF-8 はコンパイル エラーとなります。
    /// </remarks>
    public abstract class ShaderCompiler : IDisposable
    {
        #region PathCollection

        /// <summary>
        /// ファイル検索パスを管理するコレクションです。
        /// </summary>
        public sealed class PathCollection : Collection<string>
        {
            internal PathCollection() { }
        }

        #endregion

        #region CompileFlags

        /// <summary>
        /// コンパイラ フラグの列挙型です。
        /// </summary>
        [Flags]
        protected enum CompileFlags
        {
            None                            = 0,

            Debug                           = (1 << 0),
            SkipValidation                  = (1 << 1),
            SkipOptimization                = (1 << 2),
            PackMatrixRowMajor              = (1 << 3),
            PackMatrixColumnMajor           = (1 << 4),
            PartialPrecision                = (1 << 5),
            ForceVSSoftwareNoOpt            = (1 << 6),
            ForcePSSoftwareNoOpt            = (1 << 7),
            NoPreshader                     = (1 << 8),
            AvoidFlowControl                = (1 << 9),
            PreferFlowControl               = (1 << 10),
            EnableStrictness                = (1 << 11),
            EnableBackwardsCompatibility    = (1 << 12),
            IeeeStrictness                  = (1 << 13),

            OptimizationLevel0              = (1 << 14),
            OptimizationLevel1              = 0,
            OptimizationLevel2              = ((1 << 14) | (1 << 15)),
            OptimizationLevel3              = (1 << 15),

            WarningsAreErrors               = (1 << 18),
        }

        #endregion

        /// <summary>
        /// デフォルト頂点シェーダ エントリポイント。
        /// </summary>
        public const string DefaultVertexShaderEntrypoint = "VS";

        /// <summary>
        /// デフォルト ピクセル シェーダ エントリポイント。
        /// </summary>
        public const string DefaultPixelShaderEntrypoint = "PS";

        /// <summary>
        /// 実装クラス検索キー。
        /// </summary>
        public const string AppSettingKey = "Libra.Graphics.Compiler.ShaderCompiler";

        /// <summary>
        /// デフォルト実装クラス名。
        /// </summary>
        public const string DefaultImplementation = "Libra.Graphics.Compiler.SharpDX.SdxShaderCompiler, Libra.Graphics.Compiler.SharpDX, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        /// <summary>
        /// 親ファイル パス。
        /// </summary>
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

        /// <summary>
        /// 頂点シェーダ プロファイルを取得または設定します。
        /// </summary>
        public VertexShaderProfile VertexShaderProfile { get; set; }

        /// <summary>
        /// ピクセル シェーダ プロファイルを取得または設定します。
        /// </summary>
        public PixelShaderProfile PixelShaderProfile { get; set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        protected ShaderCompiler()
        {
            SystemIncludePaths = new PathCollection();
            OptimizationLevel = OptimizationLevels.Level1;
            VertexShaderProfile = VertexShaderProfile.vs_5_0;
            PixelShaderProfile = PixelShaderProfile.ps_5_0;
        }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <returns>インスタンス。</returns>
        /// <remarks>
        /// 以下の順序で実装クラスを解決してインスタンス化します。
        /// 1. app.config で AppSettingKey により定義されたクラス名
        /// 2. DefaultImplementation で定義されたデフォルト実装クラス名
        /// </remarks>
        public static ShaderCompiler CreateShaderCompiler()
        {
            // app.config 定義を参照。
            var compilerTypeName = ConfigurationManager.AppSettings[AppSettingKey];

            // app.config で未定義ならば SharpDX 実装をデフォルト指定。
            if (compilerTypeName == null)
                compilerTypeName = DefaultImplementation;

            return CreateShaderCompiler(DefaultImplementation);
        }

        /// <summary>
        /// 指定の実装クラスからインスタンスを生成します。
        /// </summary>
        /// <param name="assemblyQualifiedName">実装クラス名。</param>
        /// <returns>インスタンス。</returns>
        public static ShaderCompiler CreateShaderCompiler(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName == null) throw new ArgumentNullException("assemblyQualifiedName");

            var type = Type.GetType(assemblyQualifiedName);
            return Activator.CreateInstance(type) as ShaderCompiler;
        }

        /// <summary>
        /// 指定のアセンブリに含まれる実装クラスからインスタンスを生成します。
        /// </summary>
        /// <param name="assembly">実装クラスを含むアセンブリ。</param>
        /// <returns>インスタンス。</returns>
        public static ShaderCompiler CreateShaderCompiler(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            var type = FindShaderCompiler(assembly);
            if (type == null)
                throw new ArgumentException("ShaderCompiler not found.", "assembly");

            return Activator.CreateInstance(type) as ShaderCompiler;
        }

        /// <summary>
        /// アセンブリに含まれる実装クラスの型を検索します。
        /// </summary>
        /// <param name="assembly">アセンブリ。</param>
        /// <returns>
        /// アセンブリに含まれる実装クラスの型。アセンブリに実装クラスが存在しないならば null。
        /// </returns>
        static Type FindShaderCompiler(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return null;
            }

            foreach (var type in types)
            {
                if (!type.IsAbstract && typeof(ShaderCompiler).IsAssignableFrom(type))
                    return type;
            }

            return null;
        }

        /// <summary>
        /// シェーダ バイトコードから入力シグネチャ バイトコードを取得します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>入力シグネチャ バイトコード。</returns>
        public byte[] GetInputSignature(byte[] shaderBytecode)
        {
            if (shaderBytecode == null) throw new ArgumentNullException("shaderBytecode");

            return GetInputSignatureCore(shaderBytecode);
        }

        /// <summary>
        /// シェーダ バイトコードから出力シグネチャ バイトコードを取得します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>出力シグネチャ バイトコード。</returns>
        public byte[] GetOutputSignature(byte[] shaderBytecode)
        {
            if (shaderBytecode == null) throw new ArgumentNullException("shaderBytecode");

            return GetOutputSignatureCore(shaderBytecode);
        }

        /// <summary>
        /// シェーダ バイトコードから入出力シグネチャ バイトコードを取得します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>入出力シグネチャ バイトコード。</returns>
        public byte[] GetInputAndOutputSignature(byte[] shaderBytecode)
        {
            if (shaderBytecode == null) throw new ArgumentNullException("shaderBytecode");

            return GetInputAndOutputSignatureCore(shaderBytecode);
        }

        /// <summary>
        /// ストリームが提供する頂点シェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <param name="stream">頂点シェーダ ソースコードを提供するストリーム。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <returns>頂点シェーダ バイトコード。</returns>
        public byte[] CompileVertexShader(Stream stream, string entrypoint = DefaultVertexShaderEntrypoint)
        {
            return Compile(stream, entrypoint, ToString(VertexShaderProfile));
        }

        /// <summary>
        /// ストリームが提供するピクセル シェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <param name="stream">ピクセル シェーダ ソースコードを提供するストリーム。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <returns>ピクセル シェーダ バイトコード。</returns>
        public byte[] CompilePixelShader(Stream stream, string entrypoint = DefaultPixelShaderEntrypoint)
        {
            return Compile(stream, entrypoint, ToString(PixelShaderProfile));
        }

        /// <summary>
        /// 指定のパスにある頂点シェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <param name="path">頂点シェーダ ソースコード ファイルのパス。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <returns>頂点シェーダ バイトコード。</returns>
        public byte[] CompileVertexShader(string path, string entrypoint = DefaultVertexShaderEntrypoint)
        {
            return CompileFromFile(path, entrypoint, ToString(VertexShaderProfile));
        }

        /// <summary>
        /// 指定のパスにあるピクセル シェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <param name="path">ピクセル シェーダ ソースコード ファイルのパス。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <returns>ピクセル シェーダ バイトコード。</returns>
        public byte[] CompilePixelShader(string path, string entrypoint = DefaultPixelShaderEntrypoint)
        {
            return CompileFromFile(path, entrypoint, ToString(PixelShaderProfile));
        }

        /// <summary>
        /// 指定のパスにあるシェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <remarks>
        /// ファイルからシェーダ コードを読み込む場合、
        /// ローカル インクルード (#include "filename" 形式) は、
        /// 絶対パス指定、および、カレント ディレクトリからの相対パスに加え、
        /// 親ファイルからの相対パスも有効となります。
        /// </remarks>
        /// <param name="path">シェーダ ソースコード ファイルのパス。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <param name="profile">プロファイル。</param>
        /// <returns>シェーダ バイトコード。</returns>
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

        /// <summary>
        /// ストリームが提供するシェーダ ソースコードをコンパイルします。
        /// </summary>
        /// <remarks>
        /// ストリームからシェーダ コードを読み込む場合はファイル パスが不明となるため、
        /// ローカル インクルード (#include "filename" 形式) は、
        /// 絶対パス指定、あるいは、カレント ディレクトリからの相対パスのみが有効となります。
        /// </remarks>
        /// <param name="stream">シェーダ ソースコードを提供するストリーム。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <param name="profile">プロファイル。</param>
        /// <returns>シェーダ バイトコード。</returns>
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

            string sourceCode;
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                sourceCode = reader.ReadToEnd();
            }

            var sourceCodeBytes = Encoding.ASCII.GetBytes(sourceCode);
            var flags = ResolveCompileFlags();

            return CompileCore(sourceCodeBytes, entrypoint, profile, flags);
            
        }

        /// <summary>
        /// 実装クラスにコンパイルを委譲します。
        /// </summary>
        /// <param name="sourceCode">シェーダ ソースコード。</param>
        /// <param name="entrypoint">エントリポイント。</param>
        /// <param name="profile">プロファイル。</param>
        /// <param name="flags">コンパイル フラグ。</param>
        /// <returns>シェーダ バイトコード。</returns>
        protected abstract byte[] CompileCore(byte[] sourceCode, string entrypoint, string profile, CompileFlags flags);

        /// <summary>
        /// 実装クラスに入力シグネチャ バイトコードの抽出を委譲します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>入力シグネチャ バイトコード。</returns>
        protected abstract byte[] GetInputSignatureCore(byte[] shaderBytecode);

        /// <summary>
        /// 実装クラスに出力シグネチャ バイトコードの抽出を委譲します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>出力シグネチャ バイトコード。</returns>
        protected abstract byte[] GetOutputSignatureCore(byte[] shaderBytecode);

        /// <summary>
        /// 実装クラスに入出力シグネチャ バイトコードの抽出を委譲します。
        /// </summary>
        /// <param name="shaderBytecode">シェーダ バイトコード。</param>
        /// <returns>入出力シグネチャ バイトコード。</returns>
        protected abstract byte[] GetInputAndOutputSignatureCore(byte[] shaderBytecode);

        /// <summary>
        /// 頂点シェーダ プロファイル列挙型を文字列へ変換します。
        /// </summary>
        /// <param name="profile">頂点シェーダ プロファイル型。</param>
        /// <returns>頂点シェーダ プロファイル名。</returns>
        public static string ToString(VertexShaderProfile profile)
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
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// ピクセル シェーダ プロファイル列挙型を文字列へ変換します。
        /// </summary>
        /// <param name="profile">ピクセル シェーダ プロファイル型。</param>
        /// <returns>ピクセル シェーダ プロファイル名。</returns>
        public static string ToString(PixelShaderProfile profile)
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
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// インクルード ファイル ストリームを開きます。
        /// </summary>
        /// <param name="type">インクルードの種類 (システム インクルードあるいはローカル インクルード)。</param>
        /// <param name="fileName">インクルード ファイル名 (パス)。</param>
        /// <returns>インクルード ファイル ストリーム。</returns>
        protected Stream OpenIncludeFile(IncludeType type, string fileName)
        {
            var filePath = ResolveIncludePath(type, fileName);
            return File.OpenRead(filePath);
        }

        /// <summary>
        /// インクルード ファイル ストリームを閉じます。
        /// </summary>
        /// <param name="stream">インクルード ファイル ストリーム。</param>
        protected void CloseIncludeFile(Stream stream)
        {
            stream.Close();
        }

        /// <summary>
        /// インクルード ファイル パスを解決します。
        /// </summary>
        /// <param name="type">インクルードの種類 (システム インクルードあるいはローカル インクルード)。</param>
        /// <param name="fileName">インクルード ファイル名 (パス)。</param>
        /// <returns>解決されたインクルード ファイル パス。</returns>
        string ResolveIncludePath(IncludeType type, string fileName)
        {
            if (type == IncludeType.Local)
                return ResolveLocalIncludePath(fileName);

            return ResolveSystemIncludePath(fileName);
        }

        /// <summary>
        /// ローカル インクルード ファイル パスを解決します。
        /// </summary>
        /// <param name="fileName">インクルード ファイル名 (パス)。</param>
        /// <returns>解決されたインクルード ファイル パス。</returns>
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

        /// <summary>
        /// システム インクルード ファイル パスを解決します。
        /// </summary>
        /// <param name="fileName">インクルード ファイル名 (パス)。</param>
        /// <returns>解決されたインクルード ファイル パス。</returns>
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

        /// <summary>
        /// プロパティからコンパイル フラグを解決します。
        /// </summary>
        /// <returns>解決されたコンパイル フラグ。</returns>
        CompileFlags ResolveCompileFlags()
        {
            var flags = CompileFlags.None;

            if (EnableDebug)
                flags |= CompileFlags.Debug;

            if (SkipValidation)
                flags |= CompileFlags.SkipValidation;

            if (SkipOptimization)
                flags |= CompileFlags.SkipOptimization;

            if (PackMatrixRowMajor)
                flags |= CompileFlags.PackMatrixRowMajor;

            if (PackMatrixColumnMajor)
                flags |= CompileFlags.PackMatrixColumnMajor;

            if (EnableStrictness)
                flags |= CompileFlags.EnableStrictness;

            if (EnableBackwardsCompatibility)
                flags |= CompileFlags.EnableBackwardsCompatibility;

            switch (OptimizationLevel)
            {
                case OptimizationLevels.Level0:
                    flags |= CompileFlags.OptimizationLevel0;
                    break;
                case OptimizationLevels.Level1:
                    flags |= CompileFlags.OptimizationLevel1;
                    break;
                case OptimizationLevels.Level2:
                    flags |= CompileFlags.OptimizationLevel2;
                    break;
                case OptimizationLevels.Level3:
                    flags |= CompileFlags.OptimizationLevel3;
                    break;
            }

            if (WarningsAreErrors)
                flags |= CompileFlags.WarningsAreErrors;

            return flags;
        }

        #region IDisposable

        protected bool IsDisposed { get; private set; }

        ~ShaderCompiler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
