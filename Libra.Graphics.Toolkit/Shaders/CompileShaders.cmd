@echo off

setlocal
set error=0
set vs_profile=vs_5_0
set ps_profile=ps_5_0

::
:: Bloom PS
::
call :CompileShader BloomExtract PS %ps_profile%
call :CompileShader BloomCombine PS %ps_profile%

::
:: GaussianBlur PS
::
call :CompileShader GaussianBlur PS %ps_profile%

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
