echo off
cd explobar
set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin
msbuild explobar.csproj /t:Clean,Build /p:Configuration=Release
bin\Release\explobar.exe -config-help ..\..\docs\config-help.md
cd ..

md distro
del /q distro\*.*
copy explobar\bin\Release\* distro\

pause 