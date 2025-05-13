# stop on any non-zero exit code
$ErrorActionPreference = 'Stop'
cls

try {
    [xml]$doc = Get-Content -Path "./Directory.Build.props"
    $version = $doc.Project.PropertyGroup.Version

    # start Sonar analysis
    dotnet sonarscanner begin `
        /k:'Audit.NET-local' `
        "/d:sonar.host.url=http://localhost:9000" `
        "/v:$version" `
        "/d:sonar.token=$env:LOCAL_SONAR_TOKEN" `
        "/d:sonar.cs.vstest.reportsPaths=/test/TestResult/**/*.trx" `
        "/d:sonar.cs.opencover.reportsPaths=/test/TestResult/**/*.xml" `
        "/d:sonar.exclusions=**/templates/**,**/docs/**,**/documents/**,**/tools/**,**/packages/**" `
        "/d:sonar.coverage.exclusions=**/templates/**"

    # build solution (this is done in the Run-Test step)
    dotnet build 'Audit.NET.sln' -c Release

    # run tests
    Push-Location '.\Test'
    & .\Run-Tests.ps1
    Pop-Location

    # pause
    Read-Host 'Press Enter to continue'
    
    # end Sonar analysis
    dotnet sonarscanner end "/d:sonar.token=$env:LOCAL_SONAR_TOKEN"

    exit 0
}
catch {
    Write-Host 'ERROR' -ForegroundColor Red
    exit 1
}
