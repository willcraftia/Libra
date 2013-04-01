#region Using

using System;
using System.IO;
using Libra.Graphics.Compiler;

#endregion

namespace Tools.CompileShader
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: CompileShader <input-path> <output-path> <entrypoint> <profile>");
                return 1;
            }

            var inputPath = args[0];
            var outputPath = args[1];
            var entrypoint = args[2];
            var profile = args[3];

            try
            {
                Console.WriteLine("Compiling {0} -> {1} for {2}, {3}", inputPath, outputPath, entrypoint, profile);

                var compiler = ShaderCompiler.CreateShaderCompiler();
                compiler.EnableStrictness = true;
                compiler.OptimizationLevel = OptimizationLevels.Level3;
                compiler.WarningsAreErrors = true;

                var bytecode = compiler.CompileFromFile(inputPath, entrypoint, profile);

                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using (var stream = File.Create(outputPath))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(bytecode);
                    writer.Flush();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return 1;
            }
        }
    }
}
