$expectedToken = "571d6b80b242c87e"
$pkt = "PublicKeyToken="

# Since MongoDB.Driver is not signed (https://jira.mongodb.org/browse/CSHARP-3050)
$exclude = @( "Audit.MongoClient", "Audit.NET.MongoDB" )

$hasErrors = $false
$count = 0

Get-ChildItem -Directory -Path "..\src" -Filter "Audit.*" | ForEach-Object {
    if ($_.Name -notin $exclude) {
        Get-ChildItem -Recurse -File -Path "$($_.FullName)" -Filter "$($_.Name).dll" | ForEach-Object {
            if ($_.FullName -match "\\bin\\release\\") {
                $name = ([system.reflection.assembly]::loadfile($_.FullName)).FullName
                $token = $name.Substring($name.LastIndexOf($pkt) + $pkt.Length)
                if ($token -ne $expectedToken) {
                    Write-Host "Token for $($_.FullName) is incorrect: $($token)" -ForegroundColor Red
                    $hasErrors = $true
                } else {
                    Write-Host "Token for $($_.FullName) correct: $($token)" -ForegroundColor Green
                    $count = $count + 1
                }
            }
        }
    }
}

if ($hasErrors) {
    Write-Host "Validation failed !" -ForegroundColor Red
    Exit 1
} elseif ($count -eq 0) {
    Write-Host ""
    Write-Host "No assemblies found to validate" -ForegroundColor Red
    Exit 1
}
else {
    Write-Host "Successful public key validation of $($count) assemblies" -ForegroundColor Green
    Exit 0
}
