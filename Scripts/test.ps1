
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5129/api/Auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"password"}'
$token = $loginResponse.token
Write-Host "Got token: $token"
$headers = @{ "Authorization" = "Bearer $token" }
try {
    $result = Invoke-RestMethod -Uri "http://localhost:5129/api/AssessmentResults" -Method Get -Headers $headers
    Write-Host "Success on AssessmentResults!"
} catch {
    Write-Host "Error on AssessmentResults: $($_.Exception.Message)"
}
try {
    $result2 = Invoke-RestMethod -Uri "http://localhost:5129/api/Etr" -Method Get -Headers $headers
    Write-Host "Success on Etr!"
} catch {
    Write-Host "Error on Etr: $($_.Exception.Message)"
}

