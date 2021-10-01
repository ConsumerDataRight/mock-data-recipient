@echo off
echo Start DataRecipient projects?
pause

dotnet build ../CDR.DataRecipient.Web
pause

wt --maximized ^
--title Web -d ../CDR.DataRecipient.Web dotnet run --no-build
pause
 
 