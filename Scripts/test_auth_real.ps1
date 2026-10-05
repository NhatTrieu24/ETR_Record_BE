
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5129/api/Auth/mock-admin-token" -Method Get
$token = $loginResponse.token
Write-Host "Got token"

$headers = @{ "Authorization" = "Bearer $token" }
try {
    $res = Invoke-RestMethod -Uri "http://localhost:5129/api/AssessmentResults" -Method Get -Headers $headers
    Write-Host "Success AssessmentResults!"
} catch {
    Write-Host "Failed AssessmentResults: $($_.Exception.Message)"
}
try {
    $res2 = Invoke-RestMethod -Uri "http://localhost:5129/api/Etr" -Method Get -Headers $headers
    Write-Host "Success Etr!"
} catch {
    Write-Host "Failed Etr: $($_.Exception.Message)"
}

