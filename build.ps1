$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Error "C# Compiler (csc.exe) not found at: $csc"
    exit 1
}

$ErrorActionPreference = "Stop"

if (-not (Test-Path "app.ico")) {
    New-Item -ItemType Directory -Force -Path "obj" | Out-Null

    Write-Host "app.ico not found. Generating it from the supplied keep-alive mark..."
    & $csc /nologo /target:exe /out:obj\IconBuilder.exe /r:System.Drawing.dll tools\IconBuilder.cs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Icon builder compilation failed."
        exit $LASTEXITCODE
    }

    & .\obj\IconBuilder.exe app.ico
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Icon generation failed."
        exit $LASTEXITCODE
    }
} else {
    Write-Host "Using the supplied multi-resolution app.ico..."
}

Write-Host "Compiling BluetoothKeepAlive.exe..."
& $csc /nologo /target:winexe /win32icon:app.ico /out:BluetoothKeepAlive.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /optimize+ Program.cs

if ($LASTEXITCODE -eq 0) {
    Write-Host "Compilation successful! Generated BluetoothKeepAlive.exe" -ForegroundColor Green
} else {
    Write-Error "Compilation failed!"
    exit $LASTEXITCODE
}
