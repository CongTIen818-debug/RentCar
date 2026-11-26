param([switch]$ChayTest)
$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
dotnet dev-certs https --trust
if ($LASTEXITCODE -ne 0) { throw 'Chua tin cay chung chi HTTPS phat trien.' }
dotnet build .\RentCar.sln --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Build that bai.' }
if ($ChayTest) {
    dotnet test .\RentCar_QuanLy\tests\RentCar.Tests\RentCar.Tests.csproj --no-build --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unit test that bai.' }
}
& (Join-Path $PSScriptRoot 'chay_du_an.ps1') -KhongBuild
