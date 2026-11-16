$ErrorActionPreference = "Stop"

Write-Host "--- 1. Testing Categories ---"
$cats = Invoke-RestMethod -Uri "http://localhost:5283/api/categories" -Method Get
Write-Host "Categories Count: $($cats.data.Count)"

Write-Host "--- 2. Testing Auth Registration & Login ---"
$regBody = @{
    fullName = "Mugesh Kumar"
    email = "mugesh@example.com"
    password = "Password123!"
} | ConvertTo-Json

try {
    $reg = Invoke-RestMethod -Uri "http://localhost:5283/api/auth/register" -Method Post -Body $regBody -ContentType "application/json"
    Write-Host "Registered: $($reg.data.fullName) ($($reg.data.email))"
} catch {
    Write-Host "User might already be registered: $($_.Exception.Message)"
}

$loginBody = @{
    email = "mugesh@example.com"
    password = "Password123!"
} | ConvertTo-Json

$login = Invoke-RestMethod -Uri "http://localhost:5283/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
Write-Host "Login success: $($login.success) - Welcome $($login.data.fullName)"

Write-Host "--- 3. Testing Demo Data Loading ---"
try {
    $demo = Invoke-RestMethod -Uri "http://localhost:5283/api/settings/demo/load" -Method Post
    Write-Host "Demo load response: $($demo.message)"
} catch {
    Write-Host "Demo data might already be loaded: $($_.Exception.Message)"
}

Write-Host "--- 4. Testing Dashboard Summary ---"
$dash = Invoke-RestMethod -Uri "http://localhost:5283/api/dashboard/summary" -Method Get
Write-Host "Dashboard Net Balance: $($dash.data.totalNetBalance)"
Write-Host "Dashboard Potential Savings: $($dash.data.potentialSavings)"
Write-Host "Dashboard Actual Savings: $($dash.data.actualSavings)"
Write-Host "Dashboard Smart Insights Count: $($dash.data.smartInsights.Count)"
Write-Host "Dashboard Price Movers Count: $($dash.data.priceMovers.Count)"

Write-Host "--- 5. Testing Price Comparison ---"
$comp = Invoke-RestMethod -Uri "http://localhost:5283/api/price-comparison?product=Rice&quantity=5" -Method Get
Write-Host "Price comparison product: $($comp.data.productName)"
Write-Host "Cheapest shop: $($comp.data.cheapestShop)"
Write-Host "Lowest unit price: $($comp.data.lowestRecordedPrice)"
Write-Host "Estimated saving on 5 kg vs last purchase: $($comp.data.totalSavingVsLastPurchase)"

Write-Host "--- 6. Testing Savings Summary ---"
$sav = Invoke-RestMethod -Uri "http://localhost:5283/api/savings/summary" -Method Get
Write-Host "Actual Savings: $($sav.data.actualSavings)"
Write-Host "Potential Savings: $($sav.data.potentialSavings)"
Write-Host "Actual Savings Log Count: $($sav.data.actualSavingsLog.Count)"

Write-Host "--- 7. Testing Shopping List ---"
$addItemBody = @{
    productName = "Rice"
    quantity = 10
    unit = "kg"
} | ConvertTo-Json
$addListItem = Invoke-RestMethod -Uri "http://localhost:5283/api/shoppinglist" -Method Post -Body $addItemBody -ContentType "application/json"
$listSummary = Invoke-RestMethod -Uri "http://localhost:5283/api/shoppinglist" -Method Get
Write-Host "Shopping list items count: $($listSummary.data.items.Count)"
Write-Host "Shopping list estimated total: $($listSummary.data.totalEstimatedCost)"
Write-Host "Shopping list estimated savings vs usual: $($listSummary.data.estimatedSavingsVsUsual)"

Write-Host "--- 8. Testing Reports ---"
$rep = Invoke-RestMethod -Uri "http://localhost:5283/api/reports/monthly" -Method Get
Write-Host "Monthly report: $($rep.data.monthName), Outflow: $($rep.data.totalOutflow), Top Category: $($rep.data.topOutflowCategory)"
$prodRep = Invoke-RestMethod -Uri "http://localhost:5283/api/reports/products" -Method Get
Write-Host "Products in Price Report: $($prodRep.data.Count)"

Write-Host "--- 9. Testing Shops ---"
$shops = Invoke-RestMethod -Uri "http://localhost:5283/api/shops" -Method Get
Write-Host "Shops count: $($shops.data.Count)"
foreach ($s in $shops.data) {
    Write-Host "  - Shop: $($s.name), Spent: $($s.totalSpending), Lowest Price Matches: $($s.lowestPriceMatchesCount)"
}

Write-Host "--- ALL TESTS PASSED SUCCESSFULLY! ---"
