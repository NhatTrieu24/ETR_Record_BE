
$body = @{ idToken = "mock_token" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5129/api/Auth/google-login" -Method Post -ContentType "application/json" -Body $body
$token = $loginResponse.token
Write-Host "Got token: $token"
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

