echo off
cd src
set PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin
msbuild explobar.csproj /t:Clean,Build /p:Configuration=Release
bin\Release\explobar.exe -config-help > ..\config-help.md
cd ..
pause 