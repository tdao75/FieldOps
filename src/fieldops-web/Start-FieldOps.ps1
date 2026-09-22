$ProjectRoot = "K:\coding\FieldOps"

function Wait-ForPort {
    param(
        [int]$Port,
        [string]$ServiceName,
        [int]$TimeoutSeconds = 90
    )

    Write-Host "Waiting for $ServiceName on port $Port..." `
        -ForegroundColor Yellow

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        $available = Test-NetConnection `
            -ComputerName "localhost" `
            -Port $Port `
            -InformationLevel Quiet `
            -WarningAction SilentlyContinue

        if ($available) {
            Write-Host "$ServiceName is ready." `
                -ForegroundColor Green
            return
        }

        Start-Sleep -Seconds 2
    }

    throw "$ServiceName did not start on port $Port."
}

function Start-FieldOpsProcess {
    param(
        [string]$Title,
        [string]$Command
    )

    $startupCommand = @"
`$Host.UI.RawUI.WindowTitle = '$Title'
Set-Location '$ProjectRoot'
$Command
"@

    $commandBytes =
        [System.Text.Encoding]::Unicode.GetBytes(
            $startupCommand
        )

    $encodedCommand =
        [Convert]::ToBase64String($commandBytes)

    Start-Process powershell.exe `
        -ArgumentList @(
            "-NoExit",
            "-NoProfile",
            "-EncodedCommand",
            $encodedCommand
        )
}

Set-Location $ProjectRoot

Write-Host "Starting FieldOps..." -ForegroundColor Cyan

# Start Docker Desktop if Docker is not ready.
docker info *> $null

if ($LASTEXITCODE -ne 0) {
    $dockerDesktop =
        "C:\Program Files\Docker\Docker\Docker Desktop.exe"

    if (-not (Test-Path $dockerDesktop)) {
        throw "Docker Desktop was not found at $dockerDesktop"
    }

    Write-Host "Starting Docker Desktop..." `
        -ForegroundColor Yellow

    Start-Process $dockerDesktop

    $dockerReady = $false

    for ($attempt = 1; $attempt -le 60; $attempt++) {
        Start-Sleep -Seconds 2
        docker info *> $null

        if ($LASTEXITCODE -eq 0) {
            $dockerReady = $true
            break
        }
    }

    if (-not $dockerReady) {
        throw "Docker Desktop did not become ready."
    }
}

Write-Host "Starting PostgreSQL, RabbitMQ and Mailpit..." `
    -ForegroundColor Cyan

docker compose up -d postgres rabbitmq mailpit

if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose failed to start FieldOps infrastructure."
}

Wait-ForPort `
    -Port 5432 `
    -ServiceName "PostgreSQL"

Wait-ForPort `
    -Port 5672 `
    -ServiceName "RabbitMQ"

Wait-ForPort `
    -Port 1025 `
    -ServiceName "Mailpit"

# Start Technicians API first.
Start-FieldOpsProcess `
    -Title "FieldOps - Technicians API" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.Technicians.Api --no-launch-profile --urls http://localhost:5072
'@

Wait-ForPort `
    -Port 5072 `
    -ServiceName "Technicians API"

# Work Orders uses the Technicians API.
Start-FieldOpsProcess `
    -Title "FieldOps - Work Orders API" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.WorkOrders.Api --no-launch-profile --urls http://localhost:5062
'@

Wait-ForPort `
    -Port 5062 `
    -ServiceName "Work Orders API"

# Gateway starts after both APIs.
Start-FieldOpsProcess `
    -Title "FieldOps - API Gateway" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.ApiGateway --no-launch-profile --urls http://localhost:5080
'@

Wait-ForPort `
    -Port 5080 `
    -ServiceName "API Gateway"

# Notification worker uses RabbitMQ and Mailpit.
Start-FieldOpsProcess `
    -Title "FieldOps - Notification Worker" `
    -Command @'
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.Notification.Worker --no-launch-profile
'@

# Fix Vite to port 5173 instead of changing to 5174.
Start-FieldOpsProcess `
    -Title "FieldOps - React" `
    -Command @'
Set-Location "src\fieldops-web"
npm run dev -- --port 5174 --strictPort
'@

Wait-ForPort `
    -Port 5174 `
    -ServiceName "React"

Write-Host ""
Write-Host "FieldOps is ready." -ForegroundColor Green
Write-Host ""
Write-Host "React:     http://localhost:5174"
Write-Host "Gateway:   http://localhost:5080"
Write-Host "WorkOrders Swagger: http://localhost:5062/swagger"
Write-Host "Technicians Swagger: http://localhost:5072/swagger"
Write-Host "RabbitMQ:  http://localhost:15672"
Write-Host "Mailpit:   http://localhost:8025"

Start-Process "http://localhost:5174"