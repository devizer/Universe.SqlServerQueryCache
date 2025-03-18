Param(
  [string] $folder,
  [switch] $recurse
)
$svgFiles = Get-ChildItem -Path "$folder" -Filter "*.svg" -Recurse:$recurse
foreach($svgFile in $svgFiles) {
  $svgKey = [System.IO.Path]::GetFileNameWithoutExtension("$svgFile");
  $svgFolder = [System.IO.Path]::GetDirectoryName("$svgFile");
  $cssFile = [System.IO.Path]::Combine($svgFolder, "$($svgKey).css")
  Write-Host "$($svgFile) :--> $cssFile"
  $svgSource = [System.IO.File]::ReadAllText("$svgFile")
  $svgSourceUrlEncoded = [System.Web.HttpUtility]::UrlEncode($svgSource, (new-object System.Text.ASCIIEncoding))
  $nl=[Environment]::NewLine
  $cssSource = ".$($svgKey) {$($nl)" `
    + "  background-image: url(`"data:image/svg+xml;$svgSourceUrlEncoded`");$($nl)" `
    + "  background-repeat: no-repeat;$($nl)" `
    + "  background-size: 24px 24px;$($nl)" `
    + "  width: 24px;$($nl)" `
    + "  height: 24px;$($nl)" `
    + "  margin: auto auto;$($nl)" `
    + "}$($nl)"

  [System.IO.File]::WriteAllText("$cssFile", $cssSource)
  Write-Host ""
}
