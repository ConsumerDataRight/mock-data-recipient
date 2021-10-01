@echo off
echo Start DataRecipient projects (Build in Release configuration)?
pause

dotnet build --configuration Release ../CDR.DataRecipient.Web
pause

wt --maximized ^
--title Web -d ../CDR.DataRecipient.Web run --no-build
pause  