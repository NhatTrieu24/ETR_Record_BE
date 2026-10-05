$connStr = "Server=tcp:trieu-free-server-2026.database.windows.net,1433;Initial Catalog=ETRManagementDB(32GB);Persist Security Info=False;User ID=sqladmin;Password=@Ap090524;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
$sqlFile = "D:\Project\CapStone\ETR\ETR_Record_BE\Report\Deploy_Demo_ETR_Database_Complete.sql"

$sqlContent = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
Write-Output "Connected to Azure SQL: ETRManagementDB(32GB)"

# Split script by GO if any, or execute as one block
$batches = $sqlContent -split "(?m)^\s*GO\s*$"

$batchIndex = 0
foreach ($batch in $batches) {
    $trimmed = $batch.Trim()
    if ($trimmed.Length -eq 0) { continue }
    $batchIndex++
    Write-Output "Executing batch $batchIndex..."
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 120
    $cmd.CommandText = $trimmed
    $cmd.ExecuteNonQuery() | Out-Null
}

$conn.Close()
Write-Output "Successfully executed all batches of Deploy_Demo_ETR_Database_Complete.sql!"
