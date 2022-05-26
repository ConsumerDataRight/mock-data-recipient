#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "MockDataRecipient integration tests"
Write-Output ""
Write-Output "⚠ WARNING: Integration tests for MockDataRecipient will use the existing 'mock-register' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockRegister"
Write-Output "⚠ WARNING: Integration tests for MockDataRecipient will use the existing 'mock-data-holder' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockDataHolder"
Write-Output "⚠ WARNING: Integration tests for MockDataRecipient will use the existing 'mock-data-holder-energy' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockDataHolder Energy"
Write-Output "***********************************************************"

# Run integration tests
docker-compose -f docker-compose.IntegrationTests.yml up --build --abort-on-container-exit --exit-code-from mock-data-recipient-integration-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.IntegrationTests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: MockDataRecipient integration tests passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: MockDataRecipient integration tests failed"
    Write-Output "***********************************************************"
    exit 1
}
