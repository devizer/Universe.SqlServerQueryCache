@echo off
dotnet run -- -h
echo.
dotnet run -- -o "Query Cache Reports\LOCAL MS SQL SERVER.json" -cs "Data Source=(local); Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false"
echo.
dotnet run -- -o "Query Cache Reports\LOCAL MS SQL SERVER.json" -cs "Data Source=(local); Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false" -av
echo.

dotnet run -- -o "Query Cache Reports\LOCAL MS SQL SERVER V2.json" -s "(local)"
echo.
dotnet run -- -o "Query Cache Reports\LOCAL MS SQL SERVER V2.json" -s "(local)" -av

dotnet run -- -all -av -o "Query Cache Reports\Discovered {InstanceName}" 


