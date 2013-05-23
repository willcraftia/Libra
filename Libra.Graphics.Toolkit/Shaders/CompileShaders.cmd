@echo off

setlocal
set error=0
set vs_profile=vs_5_0
set ps_profile=ps_5_0

::
:: Filters
::
call :CompilePS BilateralFilter PS %ps_profile%
call :CompilePS BloomCombineFilter PS %ps_profile%
call :CompilePS BloomExtractFilter PS %ps_profile%
call :CompilePS CombineFilter PS %ps_profile%
call :CompilePS DirectTextureDraw PS %ps_profile%
call :CompilePS DofCombineFilter PS %ps_profile%
call :CompilePS DownFilter PS %ps_profile%
call :CompilePS EdgeFilter PS %ps_profile%
call :CompilePS GaussianFilter PS %ps_profile%
call :CompilePS HeightToNormalFilter PS %ps_profile%
call :CompilePS HeightToGradientFilter PS %ps_profile%
call :CompilePS LightScatteringFilter PS %ps_profile%
call :CompilePS LinearDepthMapColorFilter PS %ps_profile%
call :CompilePS MonochromeFilter PS %ps_profile%
call :CompilePS NormalEdgeDetectFilter PS %ps_profile%
call :CompilePS NegativeFilter PS %ps_profile%
call :CompilePS RadialFilter PS %ps_profile%
call :CompilePS ScanlineFilter PS %ps_profile%
call :CompilePS SSAOBlurFilter PS %ps_profile%
call :CompilePS SSAOCombineFilter PS %ps_profile%
call :CompilePS SSAOMapColorFilter PS %ps_profile%
call :CompilePS UpFilter PS %ps_profile%
call :CompilePS VolumetricFogCombineFilter PS %ps_profile%
call :CompilePS WaveFilter PS %ps_profile%

::
:: Effects
::
call :CompileShader DepthMap VS %vs_profile%
call :CompileShader DepthMap PS %ps_profile%
call :CompileShader LinearDepthMap VS %vs_profile%
call :CompileShader LinearDepthMap PS %ps_profile%
call :CompileShader LinearFogDepthMap VS %vs_profile%
call :CompileShader LinearFogDepthMap PS %ps_profile%
call :CompileShader NormalMap VS %vs_profile%
call :CompileShader NormalMap PS %ps_profile%
call :CompileShader ShadowMap VS %vs_profile%
call :CompileShader ShadowMap BasicPS %ps_profile%
call :CompileShader ShadowMap VariancePS %ps_profile%
call :CompileShader SingleColorObject VS %vs_profile%
call :CompileShader SingleColorObject PS %ps_profile%
call :CompileShader SSAOMap VS %vs_profile%
call :CompileShader SSAOMap PS %ps_profile%
call :CompileShader Particle VS %vs_profile%
call :CompileShader Particle PS %ps_profile%
call :CompileShader VolumetricFogMap VS %vs_profile%
call :CompileShader VolumetricFogMap PS %ps_profile%
call :CompileVS Water VS %vs_profile%
call :CompilePS Water PS %ps_profile%

::
:: FullScreenQuad
::
call :CompileVS FullScreenQuad VS %vs_profile%

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
