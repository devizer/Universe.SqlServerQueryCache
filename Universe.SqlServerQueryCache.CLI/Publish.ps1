$PUBLISH_Folder = "W:\Temp\QueryCache.CLI-PUBLISH"
$VERSION_SHORT = "1.1"
$NET6_RIDs = @("osx-x64 osx-arm64 win-x64 win-x86 win-arm64 win-arm linux-x64 linux-arm linux-arm64 linux-musl-x64 osx.10.10-x64 osx.10.11-x64".Split(" "))
$ridIndex = 0;
$ridCount = $NET6_RIDs.Count

Write-Host "BUILDING FX-DEPENDENT" -ForegroundColor Magenta
dotnet publish -f net6.0 -o $PUBLISH_Folder\fx-dependent -v:q -p:Version=$VERSION_SHORT -c Release /p:PublishSingleFile=true
Write-Host "[fx-dependent]: $(if ($?) { "SUCCESS"} Else { "FAIL"})" -ForegroundColor Magenta

foreach($rid in $NET6_RIDs) {
  Write-Host "BUILDING [$rid]" -ForegroundColor Magenta
  dotnet publish --self-contained -r $rid -f net6.0 -o $PUBLISH_Folder\$rid -v:q -p:Version=$VERSION_SHORT -c Release /p:PublishSingleFile=true
  Write-Host "[$rid]: $(if ($?) { "SUCCESS"} Else { "FAIL"})" -ForegroundColor Magenta
  Write-Host
}
