. ./Set-Variables.ps1
$branch = & { git rev-parse --abbrev-ref HEAD }
echo "Branch: [$branch]"

$commitsRaw = & { set TZ=GMT; git log -999999 --date=raw --pretty=format:"%cd" }
$lines = $commitsRaw.Split([Environment]::NewLine)
$commitCount = $lines.Length
$commitDate = $lines[0].Split(" ")[0]
echo "Commit Counter: [$commitCount]"
echo "Commit Date: [$commitDate]"

$VERSION="$BASEVER.$commitCount"
write-Host "VERSION: [$VERSION]"


