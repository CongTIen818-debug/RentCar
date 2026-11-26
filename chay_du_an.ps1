param([switch]$KhongBuild)
$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
$port = 7246
$projectPath = Join-Path $PSScriptRoot 'RentCar_TrangChu\RentCar.csproj'
$expectedExe = Join-Path $PSScriptRoot 'RentCar_TrangChu\bin\Debug\net8.0\RentCar.exe'
$expectedDll = Join-Path $PSScriptRoot 'RentCar_TrangChu\bin\Debug\net8.0\RentCar.dll'
function Test-RunningRentCar {
    $listener = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $listener) { return $false }
    $owner = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
    if ($owner.ExecutablePath -ne $expectedExe -and -not ($owner.CommandLine -and $owner.CommandLine.Contains($expectedDll))) {
        throw "Cong $port dang duoc chuong trinh khac su dung (PID $($listener.OwningProcess))."
    }
    return $true
}
if (Test-RunningRentCar) {
    Write-Host 'RentCar da chay. Khong mo them tien trinh trung cong.'
} else {
    if (-not $KhongBuild) {
        dotnet build $projectPath --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Build RentCar that bai.' }
    }
    $logPath = Join-Path $env:TEMP 'RentCar-single-host.log'
    $errorPath = Join-Path $env:TEMP 'RentCar-single-host.error.log'
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @('run','--no-build','--project',('"{0}"' -f $projectPath),'--launch-profile','https') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -RedirectStandardOutput $logPath -RedirectStandardError $errorPath -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    $ready = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-RunningRentCar) { $ready=$true; break }
        $process.Refresh()
        if ($process.HasExited) { throw "RentCar khong khoi dong duoc. Xem $errorPath va $logPath" }
        Start-Sleep -Milliseconds 300
    }
    if (-not $ready) { throw "RentCar chua san sang. Xem $logPath" }
    Write-Host 'RentCar da mo thanh cong: mot ung dung, mot cong.'
}
Write-Host 'Trang chu: https://localhost:7246'
Write-Host 'Dang nhap: https://localhost:7246/Account/Login'
Write-Host 'Quan ly: https://localhost:7246/QuanLy'
Write-Host 'Visual Studio: chi chon RentCar lam Startup Project. RentCar.Web la thu vien, khong chay rieng.'
