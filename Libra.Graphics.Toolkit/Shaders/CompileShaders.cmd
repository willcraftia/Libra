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
:: NormalMap VS/PS
::
call :CompileShader NormalMap VS %vs_profile%
call :CompileShader NormalMap PS %ps_profile%

::
:: DepthNormalMap VS/PS
::
call :CompileShader DepthNormalMap VS %vs_profile%
call :CompileShader DepthNormalMap PS %ps_profile%

::
:: SingleColorObject VS/PS
::
call :CompileShader SingleColorObject VS %vs_profile%
call :CompileShader SingleColorObject PS %ps_profile%

::
:: FullScreenQuad VS
::
call :CompileShader FullScreenQuad VS %vs_profile%

::
:: Combine PS
::
call :CompileShader Combine PS %ps_profile%

::
:: DownFilter PS
::
call :CompileShader DownFilter PS %ps_profile%

::
:: UpFilter PS
::
call :CompileShader UpFilter PS %ps_profile%

::
:: GaussianBlur PS
::
call :CompileShader GaussianBlur PS %ps_profile%

::
:: RadialBlur PS
::
call :CompileShader RadialBlur PS %ps_profile%

::
:: BloomExtract PS
::
call :CompileShader BloomExtract PS %ps_profile%

::
:: BloomCombine PS
::
call :CompileShader BloomCombine PS %ps_profile%

::
:: DofCombine PS
::
call :CompileShader DofCombine PS %ps_profile%

::
:: LightScattering PS
::
call :CompileShader LightScattering PS %ps_profile%

::
:: DirectTextureDraw PS
::
call :CompileShader DirectTextureDraw PS %ps_profile%

::
:: Monochrome PS
::
call :CompileShader Monochrome PS %ps_profile%

::
:: Scanline PS
::
call :CompileShader Scanline PS %ps_profile%

::
:: Edge PS
::
call :CompileShader Edge PS %ps_profile%

::
:: NegativeFilter PS
::
call :CompileShader NegativeFilter PS %ps_profile%

::
:: NormalEdgeDetect PS
::
call :CompileShader NormalEdgeDetect PS %ps_profile%

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
