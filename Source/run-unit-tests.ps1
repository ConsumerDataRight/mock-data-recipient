#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "MockDataRecipient unit tests"
Write-Output "***********************************************************"

# Build and run containers
 docker-compose -f docker-compose.UnitTests.yml up --build --abort-on-container-exit --exit-code-from mock-data-recipient-unit-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.UnitTests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: MockDataRecipient unit tests passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: MockDataRecipient unit tests failed"
    Write-Output "***********************************************************"
    exit 1
}
