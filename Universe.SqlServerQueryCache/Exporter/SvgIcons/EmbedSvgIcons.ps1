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
  $svgSourceUrlEncoded = "data:image/svg+xml,$svgSourceUrlEncoded"
  $bytes = (new-object System.Text.Utf8Encoding($false)).GetBytes($svgSource)
  $svgSourceBase64 = [System.Convert]::ToBase64String($bytes)
  $svgSourceBase64 = "data:image/svg+xml;base64,$svgSourceBase64"


  $nl=[Environment]::NewLine
  $cssSource = ".$($svgKey) {$($nl)" `
    + "  background-image: url(`"$svgSourceBase64`");$($nl)" `
    + "}$($nl)"

  [System.IO.File]::WriteAllText("$cssFile", $cssSource)
  Write-Host ""
}
