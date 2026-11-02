param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../../device-emulator/bin/Debug/net8.0-windows')
)

$ErrorActionPreference = 'Stop'
$outputPath = (Resolve-Path -LiteralPath $OutputDirectory).Path
$executable = Join-Path $outputPath 'DeviceEmulator.exe'
$library = Join-Path $outputPath 'GeneralLibrary.dll'
if (!(Test-Path -LiteralPath $library)) {
    throw 'GeneralLibrary.dll must remain beside DeviceEmulator.exe so CLR can load the App base class before Costura initializes.'
}

$startedAt = Get-Date
$process = Start-Process -FilePath $executable -WorkingDirectory $outputPath -WindowStyle Hidden -PassThru
try {
    $deadline = $startedAt.AddSeconds(25)
    do {
        $process.Refresh()
        if ($process.HasExited) {
            throw "DeviceEmulator exited during startup with code $($process.ExitCode). Check Log/Startup.log."
        }
        if ($process.MainWindowHandle -ne 0 -and $process.MainWindowTitle -like '*下位机模拟*') {
            break
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)

    if ($process.MainWindowHandle -eq 0 -or $process.WaitForExit(1000)) {
        throw 'DeviceEmulator did not reach a stable main window.'
    }
    $startupLog = Get-ChildItem -LiteralPath (Join-Path $outputPath 'Log') -Filter '*Info*.log' |
        Where-Object { $_.LastWriteTime -ge $startedAt } |
        Select-String -Pattern 'DeviceEmulator 启动，版本'
    if (!$startupLog) {
        throw 'Startup succeeded but no startup information log was written.'
    }
    Write-Output "PASS: packaged executable starts, main window is ready, startup log exists. Title: $($process.MainWindowTitle)"
}
finally {
    if (!$process.HasExited) {
        $null = $process.CloseMainWindow()
        if (!$process.WaitForExit(5000)) {
            $process.Kill()
        }
    }
    $process.Dispose()
}
