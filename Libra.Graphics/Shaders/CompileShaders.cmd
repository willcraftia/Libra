@echo off

setlocal
set error=0
set vs_profile=vs_5_0
set ps_profile=ps_5_0

::
:: BasicEffect VS
::
call :CompileShader BasicEffect VSBasic %vs_profile%
call :CompileShader BasicEffect VSBasicNoFog %vs_profile%
call :CompileShader BasicEffect VSBasicVc %vs_profile%
call :CompileShader BasicEffect VSBasicVcNoFog %vs_profile%
call :CompileShader BasicEffect VSBasicTx %vs_profile%
call :CompileShader BasicEffect VSBasicTxNoFog %vs_profile%
call :CompileShader BasicEffect VSBasicTxVc %vs_profile%
call :CompileShader BasicEffect VSBasicTxVcNoFog %vs_profile%

call :CompileShader BasicEffect VSBasicVertexLighting %vs_profile%
call :CompileShader BasicEffect VSBasicVertexLightingVc %vs_profile%
call :CompileShader BasicEffect VSBasicVertexLightingTx %vs_profile%
call :CompileShader BasicEffect VSBasicVertexLightingTxVc %vs_profile%

call :CompileShader BasicEffect VSBasicOneLight %vs_profile%
call :CompileShader BasicEffect VSBasicOneLightVc %vs_profile%
call :CompileShader BasicEffect VSBasicOneLightTx %vs_profile%
call :CompileShader BasicEffect VSBasicOneLightTxVc %vs_profile%

call :CompileShader BasicEffect VSBasicPixelLighting %vs_profile%
call :CompileShader BasicEffect VSBasicPixelLightingVc %vs_profile%
call :CompileShader BasicEffect VSBasicPixelLightingTx %vs_profile%
call :CompileShader BasicEffect VSBasicPixelLightingTxVc %vs_profile%

::
:: BasicEffect PS
::
call :CompileShader BasicEffect PSBasic %ps_profile%
call :CompileShader BasicEffect PSBasicNoFog %ps_profile%
call :CompileShader BasicEffect PSBasicPixelLighting %ps_profile%
call :CompileShader BasicEffect PSBasicPixelLightingTx %ps_profile%
call :CompileShader BasicEffect PSBasicTx %ps_profile%
call :CompileShader BasicEffect PSBasicTxNoFog %ps_profile%
call :CompileShader BasicEffect PSBasicVertexLighting %ps_profile%
call :CompileShader BasicEffect PSBasicVertexLightingNoFog %ps_profile%
call :CompileShader BasicEffect PSBasicVertexLightingTx %ps_profile%
call :CompileShader BasicEffect PSBasicVertexLightingTxNoFog %ps_profile%

::
:: SpriteEffect VS
::
call :CompileShader SpriteEffect VS %vs_profile%

::
:: SpriteEffect PS
::
call :CompileShader SpriteEffect PS %ps_profile%

echo.

if %error% == 0 (
    echo Shaders compiled ok
) else (
    echo There were shader compilation errors!
)

endlocal
exit /b

:CompileShader
set compiler=..\..\Tools.CompileShader\bin\Debug\Tools.CompileShader.exe %1.fx Compiled\%1%2.bin %2 %3
echo.
echo %compiler%
%compiler% || set error=1
exit /b
