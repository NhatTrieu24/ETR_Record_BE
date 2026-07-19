
# Step 1: Create a valid token directly from the API by skipping google-login and using the standard login
$body = @{ username = "admin"; password = "password" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5129/api/Auth/login" -Method Post -ContentType "application/json" -Body $body
$token = $loginResponse.token
Write-Host "Got token: $token"

$headers = @{ "Authorization" = "Bearer $token" }
try {
    $res = Invoke-RestMethod -Uri "http://localhost:5129/api/AssessmentResults" -Method Get -Headers $headers
    Write-Host "Success AssessmentResults!"
} catch {
    Write-Host "Failed AssessmentResults: $($_.Exception.Message)"
}

