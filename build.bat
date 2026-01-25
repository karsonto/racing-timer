@echo off
chcp 65001 >nul
echo ========================================
echo   比赛计时系统 - 本地打包脚本
echo ========================================
echo.

:: 获取脚本所在目录
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

:: 关闭可能运行的应用
echo [1/3] 关闭运行中的应用...
taskkill /F /IM Timer.exe 2>nul
echo.

:: 发布最新代码
echo [2/3] 发布最新代码...
cd /d "%SCRIPT_DIR%Timer\Timer"
dotnet publish -c Release -r win-x64 --self-contained true -o publish
if %errorlevel% neq 0 (
    echo 发布失败！
    pause
    exit /b 1
)
echo.

:: 打包安装程序
echo [3/3] 打包安装程序...
cd /d "%SCRIPT_DIR%"

:: 创建输出目录
if not exist "installer" mkdir installer

:: 尝试常见的 Inno Setup 安装路径
set "ISCC_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
) else if exist "D:\Inno Setup 6\ISCC.exe" (
    set "ISCC_PATH=D:\Inno Setup 6\ISCC.exe"
)

if "%ISCC_PATH%"=="" (
    echo 错误：未找到 Inno Setup！
    echo 请从 https://jrsoftware.org/isinfo.php 下载并安装
    pause
    exit /b 1
)

"%ISCC_PATH%" "%SCRIPT_DIR%setup.iss"
if %errorlevel% neq 0 (
    echo 打包失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo   打包完成！
echo   安装包位置: %SCRIPT_DIR%installer\
echo ========================================
echo.

:: 列出生成的文件
dir /b "%SCRIPT_DIR%installer\*.exe"
echo.
pause




