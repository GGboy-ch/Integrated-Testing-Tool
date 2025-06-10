@echo off
REM 设置编译器路径（若未加入环境变量）
set ISS_COMPILER="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

REM 执行编译（静默模式，输出日志）
%ISS_COMPILER% /Q /O"Output\Setup.exe" "C:\Users\wayzim\Desktop\Tools\SetupTool\PackTool.iss" > build.log 2>&1

REM 检查编译结果
if %errorlevel% equ 0 (
    echo [成功] 编译完成，输出文件：Output\Setup.exe
) else (
    echo [失败] 编译出错，请查看 build.log
)
pause