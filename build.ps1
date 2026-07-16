$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Error "C# Compiler (csc.exe) not found at: $csc"
    exit 1
}

if (-not (Test-Path "app.ico")) {
    Write-Error "Application icon not found: app.ico"
    exit 1
}

Write-Host "Compiling BluetoothKeepAlive.exe with app.ico..."
& $csc /nologo /target:winexe /win32icon:app.ico /out:BluetoothKeepAlive.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /optimize+ Program.cs

if ($LASTEXITCODE -eq 0) {
    Write-Host "Compilation successful! Generated BluetoothKeepAlive.exe" -ForegroundColor Green
} else {
    Write-Error "Compilation failed!"
    exit $LASTEXITCODE
}
