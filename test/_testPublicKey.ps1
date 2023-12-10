$expectedToken = "571d6b80b242c87e"
$pkt = "PublicKeyToken="

# Since MongoDB.Driver is not signed (https://jira.mongodb.org/browse/CSHARP-3050)
$exclude = @( "Audit.MongoClient", "Audit.NET.MongoDB" )

$hasErrors = $false

Get-ChildItem -Directory -Path "..\src" -Filter "Audit.*" | ForEach-Object {
    if ($_.Name -notin $exclude) {
        Get-ChildItem -Recurse -File -Path "$($_.FullName)" -Filter "$($_.Name).dll" | ForEach-Object {
            if ($_.FullName -match "\\bin\\release\\") {
                $name = ([system.reflection.assembly]::loadfile($_.FullName)).FullName
                $token = $name.Substring($name.LastIndexOf($pkt) + $pkt.Length)
                if ($token -ne $expectedToken) {
                    Write-Host "Token for $($_.FullName) is incorrect: $($token)" -ForegroundColor Red
                    $hasErrors = $true
                }
            }
        }
    }
}

if ($hasErrors) {
    Exit 1
} else {
    Write-Host "Successful public key validation" -ForegroundColor Green
    Exit 0
}
