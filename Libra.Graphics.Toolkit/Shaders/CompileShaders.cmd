@echo off

setlocal
set error=0
set vs_profile=vs_5_0
set ps_profile=ps_5_0

::
:: ShadowMap VS/PS
::
call :CompileShader ShadowMap VS %vs_profile%
call :CompileShader ShadowMap BasicPS %ps_profile%
call :CompileShader ShadowMap VariancePS %ps_profile%

::
:: DepthMap VS/PS
::
call :CompileShader DepthMap VS %vs_profile%
call :CompileShader DepthMap PS %ps_profile%

::
:: LinearDepthMap VS/PS
::
call :CompileShader LinearDepthMap VS %vs_profile%
call :CompileShader LinearDepthMap PS %ps_profile%

::
:: NormalMap VS/PS
::
call :CompileShader NormalMap VS %vs_profile%
call :CompileShader NormalMap PS %ps_profile%

::
:: SSAOMap VS/PS
::
call :CompileShader SSAOMap VS %vs_profile%
call :CompileShader SSAOMap PS %ps_profile%

::
:: SSAOBlurFilter PS
::
call :CompileShader SSAOBlurFilter PS %ps_profile%

::
:: SSAOCombineFilter PS
::
call :CompileShader SSAOCombineFilter PS %ps_profile%

::
:: LinearFogDepthMap VS/PS
::
call :CompileShader LinearFogDepthMap VS %vs_profile%
call :CompileShader LinearFogDepthMap PS %ps_profile%

::
:: VolumetricFogMap VS/PS
::
call :CompileShader VolumetricFogMap VS %vs_profile%
call :CompileShader VolumetricFogMap PS %ps_profile%

::
:: VolumetricFogCombineFilter PS
::
call :CompileShader VolumetricFogCombineFilter PS %ps_profile%

::
:: SingleColorObject VS/PS
::
call :CompileShader SingleColorObject VS %vs_profile%
call :CompileShader SingleColorObject PS %ps_profile%

::
:: Particle VS/PS
::
call :CompileShader Particle VS %vs_profile%
call :CompileShader Particle PS %ps_profile%

::
:: FullScreenQuad VS
::
call :CompileShader FullScreenQuad VS %vs_profile%

::
:: CombineFilter PS
::
call :CompileShader CombineFilter PS %ps_profile%

::
:: DownFilter PS
::
call :CompileShader DownFilter PS %ps_profile%

::
:: UpFilter PS
::
call :CompileShader UpFilter PS %ps_profile%

::
:: GaussianFilter PS
::
call :CompileShader GaussianFilter PS %ps_profile%

::
:: RadialFilter PS
::
call :CompileShader RadialFilter PS %ps_profile%

::
:: BilateralFilter PS
::
call :CompileShader BilateralFilter PS %ps_profile%

::
:: BloomExtractFilter PS
::
call :CompileShader BloomExtractFilter PS %ps_profile%

::
:: BloomCombineFilter PS
::
call :CompileShader BloomCombineFilter PS %ps_profile%

::
:: DofCombineFilter PS
::
call :CompileShader DofCombineFilter PS %ps_profile%

::
:: LightScatteringFilter PS
::
call :CompileShader LightScatteringFilter PS %ps_profile%

::
:: DirectTextureDraw PS
::
call :CompileShader DirectTextureDraw PS %ps_profile%

::
:: MonochromeFilter PS
::
call :CompileShader MonochromeFilter PS %ps_profile%

::
:: ScanlineFilter PS
::
call :CompileShader ScanlineFilter PS %ps_profile%

::
:: EdgeFilter PS
::
call :CompileShader EdgeFilter PS %ps_profile%

::
:: NegativeFilter PS
::
call :CompileShader NegativeFilter PS %ps_profile%

::
:: NormalEdgeDetectFilter PS
::
call :CompileShader NormalEdgeDetectFilter PS %ps_profile%

::
:: LinearDepthMapColorFilter PS
::
call :CompileShader LinearDepthMapColorFilter PS %ps_profile%

::
:: SSAOMapColorFilter PS
::
call :CompileShader SSAOMapColorFilter PS %ps_profile%

::
:: WaveFilter PS
::
call :CompileShader WaveFilter PS %ps_profile%

::
:: HeightToNormalFilter PS
::
call :CompileShader HeightToNormalFilter PS %ps_profile%

::
:: HeightToGradientFilter PS
::
call :CompileShader HeightToGradientFilter PS %ps_profile%

::
:: Water VS/PS
::
call :CompileVS Water VS %vs_profile%
call :CompilePS Water PS %ps_profile%

echo.

if %error% == 0 (
    echo Shaders compiled ok
) else (
    echo There were shader compilation errors!
)

endlocal
exit /b

:CompileShader
set compiler=..\..\Tools.CompileShader\bin\Debug\Tools.CompileShader.exe %1.hlsl Compiled\%1%2.bin %2 %3
echo.
echo %compiler%
%compiler% || set error=1
exit /b

:CompileVS
set compiler=..\..\Tools.CompileShader\bin\Debug\Tools.CompileShader.exe %1.vs Compiled\%1%2.bin %2 %3
echo.
echo %compiler%
%compiler% || set error=1
exit /b

:CompilePS
set compiler=..\..\Tools.CompileShader\bin\Debug\Tools.CompileShader.exe %1.ps Compiled\%1%2.bin %2 %3
echo.
echo %compiler%
%compiler% || set error=1
exit /b
