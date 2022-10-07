param([Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][String[]]$projects,
[Parameter(Mandatory=$false)][String]$extraParams='',
[Parameter(Mandatory=$false)][String]$title='',
[Parameter(Mandatory=$false)][switch]$nopause = $false,
[Parameter(Mandatory=$false)][int32]$delay = 0) 

$host.ui.RawUI.WindowTitle = "RUN: $title";

if ($delay -ne 0) {
	Write-Host "Will wait $delay seconds before start..."
	Start-Sleep #delay
}

Write-Host "`r`n`r`n RUNNING $title UNIT TESTS `r`n`r`n" -foregroundcolor white -BackgroundColor DarkCyan

$totalProjs = $projects.Count;

if ($totalProjs -eq 0) {
    Write-Output "Wrong parameters"
    Exit 1
}

$hasFailed = $false;

$projects | ForEach {
    & dotnet test $_ --"logger:console;verbosity=normal" --no-build -c Release $extraParams
    if ($LASTEXITCODE -ne 0) {
        $hasFailed = $true;
    }
}

Write-Host ""
if ($hasFailed) {
    $host.ui.RawUI.WindowTitle = "FAIL: $title"
    Write-Host "   At least one test has Failed !!!   " -foregroundcolor white -BackgroundColor red
} else {
    $host.ui.RawUI.WindowTitle = "OK: $title"
    Write-Host "   Completed Sucessfully !!!   " -foregroundcolor white -BackgroundColor green
}
Write-Host ""

if ($nopause -eq $false) {
    & pause
}

if ($hasFailed) {
    Exit 1
} else {
    Exit 0
}