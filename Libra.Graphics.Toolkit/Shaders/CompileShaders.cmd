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
call :CompilePS CloudLayerFilter PS %ps_profile%
call :CompilePS CombineFilter PS %ps_profile%
call :CompilePS DofCombineFilter PS %ps_profile%
call :CompilePS DownFilter PS %ps_profile%
call :CompilePS EdgeFilter PS %ps_profile%
call :CompilePS ExponentialFogFilter PS %ps_profile%
call :CompilePS FluidRippleFilter PS %ps_profile%
call :CompilePS GaussianFilter PS %ps_profile%
call :CompilePS HeightFogFilter PS %ps_profile%
call :CompilePS LightScatteringFilter PS %ps_profile%
call :CompilePS LinearDepthMapColorFilter PS %ps_profile%
call :CompilePS LinearFogFilter PS %ps_profile%
call :CompilePS MonochromeFilter PS %ps_profile%
call :CompilePS NormalBilateralFilter PS %ps_profile%
call :CompilePS NormalDepthBilateralFilter PS %ps_profile%
call :CompilePS NormalEdgeDetectFilter PS %ps_profile%
call :CompilePS NegativeFilter PS %ps_profile%
call :CompilePS OcclusionCombineFilter PS %ps_profile%
call :CompilePS OcclusionMapColorFilter PS %ps_profile%
call :CompilePS OcclusionMergeFilter PS %ps_profile%
call :CompilePS RadialFilter PS %ps_profile%
call :CompilePS ScanlineFilter PS %ps_profile%
call :CompilePS UpFilter PS %ps_profile%
call :CompilePS VolumetricFogCombineFilter PS %ps_profile%

::
:: Converters
::
call :CompilePS HeightToNormalFilter PS %ps_profile%
call :CompilePS HeightToGradientFilter PS %ps_profile%

::
:: Effect like
::
call :CompileVS Cloud VS %vs_profile%
call :CompilePS Cloud PS %ps_profile%
call :CompileVS DepthMap VS %vs_profile%
call :CompilePS DepthMap PS %ps_profile%
call :CompilePS DepthVarianceMap PS %ps_profile%
call :CompileVS Fluid VS %vs_profile%
call :CompilePS Fluid PS %ps_profile%
call :CompileVS LinearDepthMap VS %vs_profile%
call :CompilePS LinearDepthMap PS %ps_profile%
call :CompileVS LinearFogDepthMap VS %vs_profile%
call :CompilePS LinearFogDepthMap PS %ps_profile%
call :CompileVS NormalMap VS %vs_profile%
call :CompilePS NormalMap PS %ps_profile%
call :CompilePS ShadowSceneMap BasicPS %ps_profile%
call :CompilePS ShadowSceneMap VariancePS %ps_profile%
call :CompilePS ShadowSceneMap PcfPS %ps_profile%
call :CompileVS SingleColorObject VS %vs_profile%
call :CompilePS SingleColorObject PS %ps_profile%
call :CompilePS SSAOMap PS %ps_profile%
call :CompileVS Particle VS %vs_profile%
call :CompilePS Particle PS %ps_profile%
call :CompileVS VolumetricFogMap VS %vs_profile%
call :CompilePS VolumetricFogMap PS %ps_profile%

::
:: FullScreenQuad
::
call :CompileVS FullScreenQuad VS %vs_profile%
call :CompileVS FullScreenQuadViewRay VS %vs_profile%

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
