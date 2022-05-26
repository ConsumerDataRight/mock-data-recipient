@echo off
echo Run solutions from .Net CLI using localhost and localdb from appsettings.Development.json
pause

setx ASPNETCORE_ENVIRONMENT Development

dotnet build CDR.DataRecipient.WEB

wt --maximized ^
--title MDR_WEB -d CDR.DataRecipient.WEB dotnet run --no-launch-profile