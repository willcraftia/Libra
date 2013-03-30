#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public abstract class VertexShader : Shader
    {
        // VertexShader の破棄と同時に InputLayout も破棄したいため、
        // VertexShader で InputLayout のキャッシュを管理。

        Dictionary<VertexDeclaration, InputLayout> inputLayoutMap;

        protected VertexShader(IDevice device)
            : base(device)
        {
            inputLayoutMap = new Dictionary<VertexDeclaration, InputLayout>();
        }

        // 入力スロット #0 のみを対象とした入力レイアウトの自動解決とキャッシュ。

        public InputLayout GetInputLayout(VertexDeclaration vertexDeclaration)
        {
            if (!initialized) throw new InvalidOperationException("Not initialized.");
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");

            lock (inputLayoutMap)
            {
                InputLayout inputLayout;
                if (!inputLayoutMap.TryGetValue(vertexDeclaration, out inputLayout))
                {
                    // 入力スロット #0 固定。
                    inputLayout = Device.CreateInputLayout();
                    inputLayout.Initialize(ShaderBytecode, vertexDeclaration);

                    inputLayoutMap[vertexDeclaration] = inputLayout;
                }

                return inputLayout;
            }
        }
    }
}
