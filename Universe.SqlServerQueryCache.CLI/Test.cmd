@echo off
set NETV=net6.0
dotnet run -f %NETV% -- -h
echo.
dotnet run -f %NETV% -- -o "Query Cache Reports\LOCAL MS SQL SERVER.json" -cs "Data Source=(local); Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false"
echo.
dotnet run -f %NETV% -- -o "Query Cache Reports\LOCAL MS SQL SERVER.json" -cs "Data Source=(local); Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false" -av
echo.

dotnet run -f %NETV% -- -o "Query Cache Reports\LOCAL MS SQL SERVER V2.json" -s "(local)"
echo.
dotnet run -f %NETV% -- -o "Query Cache Reports\LOCAL MS SQL SERVER V2.json" -s "(local)" -av

dotnet run -f %NETV% -- -all -av -o "Query Cache Reports\Discovered {InstanceNaMe}" 


