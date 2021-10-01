@echo off
cls
echo Run tests?
pause
dotnet test --logger "console;verbosity=detailed" > _temp/20.log