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

function Wait-ForHttp {
    param(
        [string]$Url,
        [string]$ServiceName,
        [int]$TimeoutSeconds = 120
    )

    Write-Host "Waiting for $ServiceName at $Url..." `
        -ForegroundColor Yellow

    $deadline =
        (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest `
                -Uri $Url `
                -UseBasicParsing `
                -TimeoutSec 3 `
                -ErrorAction Stop

            if ($response.StatusCode -eq 200) {
                Write-Host "$ServiceName is healthy." `
                    -ForegroundColor Green
                return
            }
        }
        catch {
            # The service is still starting.
        }

        Start-Sleep -Seconds 2
    }

    throw "$ServiceName did not become healthy at $Url."
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

Write-Host "Starting PostgreSQL, RabbitMQ and Mailpit and pgAdmin..." `
    -ForegroundColor Cyan

docker compose up -d postgres rabbitmq mailpit pgadmin

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
	
Wait-ForPort `
    -Port 5050 `
    -ServiceName "pgAdmin" `
    -TimeoutSeconds 120

# Start Technicians API first.
Start-FieldOpsProcess `
    -Title "FieldOps - Technicians API" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.Technicians.Api --no-launch-profile --urls http://localhost:5072
'@

Wait-ForHttp `
    -Url "http://localhost:5072/health" `
    -ServiceName "Technicians API"

# Work Orders uses the Technicians API.
Start-FieldOpsProcess `
    -Title "FieldOps - Work Orders API" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.WorkOrders.Api --no-launch-profile --urls http://localhost:5062
'@

Wait-ForHttp `
    -Url "http://localhost:5062/health" `
    -ServiceName "Work Orders API"

# Identity API uses PostgreSQL.
Start-FieldOpsProcess `
    -Title "FieldOps - Identity API" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.Identity.Api --no-launch-profile --urls http://localhost:5090
'@

Wait-ForHttp `
    -Url "http://localhost:5090/health" `
    -ServiceName "Identity API"
	
# Gateway starts after both APIs.
Start-FieldOpsProcess `
    -Title "FieldOps - API Gateway" `
    -Command @'
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.ApiGateway --no-launch-profile --urls http://localhost:5080
'@

Wait-ForHttp `
    -Url "http://localhost:5080/health" `
    -ServiceName "API Gateway"

# Notification worker uses RabbitMQ and Mailpit.
Start-FieldOpsProcess `
    -Title "FieldOps - Notification Worker" `
    -Command @'
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project src\FieldOps.Notification.Worker --no-launch-profile
'@

# Use port 5174 because InterviewTracker uses port 5173.
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
Write-Host "Identity Swagger: http://localhost:5090/swagger"
Write-Host "WorkOrders Swagger: http://localhost:5062/swagger"
Write-Host "Technicians Swagger: http://localhost:5072/swagger"
Write-Host "RabbitMQ:  http://localhost:15672"
Write-Host "Mailpit:   http://localhost:8025"
Write-Host "pgAdmin:   http://localhost:5050"

Start-Process "http://localhost:5174"