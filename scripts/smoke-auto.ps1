$ErrorActionPreference = "Stop"

$base = "http://localhost:5078"

function Test-Step {
    param(
        [string]$Name,
        [scriptblock]$Body
    )

    try {
        $result = & $Body
        Write-Output ("PASS: {0} -> {1}" -f $Name, $result)
    }
    catch {
        Write-Output ("FAIL: {0} -> {1}" -f $Name, $_.Exception.Message)
    }
}

$email = "autotest_{0}@local" -f ([guid]::NewGuid().ToString("N").Substring(0, 8))
$pwd = "User123!"

Test-Step "dev status" {
    (Invoke-RestMethod -Uri "$base/api/dev/status" -Method GET).isDevelopment
}

Test-Step "register user" {
    $body = @{ email = $email; password = $pwd } | ConvertTo-Json
    (Invoke-RestMethod -Uri "$base/api/auth/register" -Method POST -ContentType "application/json" -Body $body).isSuccess
}

$userLogin = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType "application/json" -Body (@{ email = $email; password = $pwd } | ConvertTo-Json)
$adminLogin = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType "application/json" -Body (@{ email = "admin@local"; password = "Admin123!" } | ConvertTo-Json)

$userToken = $userLogin.data.token
$adminToken = $adminLogin.data.token
$userHeaders = @{ Authorization = "Bearer $userToken" }
$adminHeaders = @{ Authorization = "Bearer $adminToken" }

Test-Step "login user token" { -not [string]::IsNullOrWhiteSpace($userToken) }
Test-Step "login admin token" { -not [string]::IsNullOrWhiteSpace($adminToken) }

$contents = Invoke-RestMethod -Uri "$base/api/content" -Method GET
Test-Step "content load" { "count=$($contents.Count)" }

$contentId = $contents[0].id

Test-Step "log view" {
    (Invoke-RestMethod -Uri "$base/api/useractions/log" -Method POST -Headers $userHeaders -ContentType "application/json" -Body (@{ contentId = $contentId; type = 0 } | ConvertTo-Json)).message
}
Test-Step "log like" {
    (Invoke-RestMethod -Uri "$base/api/useractions/log" -Method POST -Headers $userHeaders -ContentType "application/json" -Body (@{ contentId = $contentId; type = 1 } | ConvertTo-Json)).message
}
Test-Step "log click" {
    (Invoke-RestMethod -Uri "$base/api/useractions/log" -Method POST -Headers $userHeaders -ContentType "application/json" -Body (@{ contentId = $contentId; type = 2 } | ConvertTo-Json)).message
}

$popular = Invoke-RestMethod -Uri "$base/api/recommendations/popular?limit=5" -Method GET
$bycat = Invoke-RestMethod -Uri "$base/api/recommendations/by-categories?limit=5" -Headers $userHeaders -Method GET
$knn = Invoke-RestMethod -Uri "$base/api/recommendations/knn?limit=5" -Headers $userHeaders -Method GET

Test-Step "popular recs" { "count=$($popular.Count)" }
Test-Step "by-categories recs" { "count=$($bycat.Count)" }
Test-Step "knn recs" { "count=$($knn.Count)" }

$exp = Invoke-RestMethod -Uri "$base/api/recommendations/explain?algorithm=knn&limit=5" -Headers $userHeaders -Method GET
Test-Step "explainability" { "count=$($exp.Count)" }

$profile = Invoke-RestMethod -Uri "$base/api/useractions/interest-profile?top=5" -Headers $userHeaders -Method GET
Test-Step "interest profile" { "actions=$($profile.totalActions) cats=$($profile.topCategories.Count) tags=$($profile.topTags.Count)" }

$demo = Invoke-RestMethod -Uri "$base/api/dev/generate-demo-history" -Headers $userHeaders -Method POST
Test-Step "demo history" { "$($demo.message) added=$($demo.added)" }

$users = Invoke-RestMethod -Uri "$base/api/admin/users" -Headers $adminHeaders -Method GET
Test-Step "admin users list" { "count=$($users.Count)" }

$userId = [string]$users[0].id
$metrics = Invoke-RestMethod -Uri "$base/api/admin/metrics/recommendations?userId=$userId&algorithm=knn&k=5" -Headers $adminHeaders -Method GET
Test-Step "admin metrics" { "P@K=$($metrics.precisionAtK) R@K=$($metrics.recallAtK) NDCG=$($metrics.ndcgAtK)" }

$ov = Invoke-RestMethod -Uri "$base/api/admin/db/overview" -Headers $adminHeaders -Method GET
Test-Step "db overview" { "users=$($ov.users) actions=$($ov.actions)" }

$dbUsers = Invoke-RestMethod -Uri "$base/api/admin/db/users?limit=5" -Headers $adminHeaders -Method GET
Test-Step "db users rows" { "count=$($dbUsers.Count)" }

$dbActions = Invoke-RestMethod -Uri "$base/api/admin/db/actions?limit=5" -Headers $adminHeaders -Method GET
Test-Step "db actions rows" { "count=$($dbActions.Count)" }

Test-Step "export csv" {
    $resp = Invoke-WebRequest -Uri "$base/api/admin/reports/export/csv" -Headers $adminHeaders -Method GET
    "status=$($resp.StatusCode) bytes=$($resp.Content.Length)"
}

Test-Step "export pdf" {
    $resp = Invoke-WebRequest -Uri "$base/api/admin/reports/export/pdf" -Headers $adminHeaders -Method GET
    "status=$($resp.StatusCode) bytes=$($resp.Content.Length)"
}

$same = ((@($popular | ForEach-Object { $_.contentId }) -join ",") -eq (@($knn | ForEach-Object { $_.contentId }) -join ","))
Write-Output ("INFO: popular_vs_knn_same={0}" -f $same)
