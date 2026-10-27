[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$ControlPort = 8890,

    [ValidateRange(1, 30)]
    [int]$TimeoutSeconds = 5
)

$endpointUri = "http://127.0.0.1:$ControlPort/control/shutdown"

try {
    $response = Invoke-WebRequest `
        -Uri $endpointUri `
        -Method Post `
        -TimeoutSec $TimeoutSeconds `
        -UseBasicParsing

    if ([int]$response.StatusCode -ne 202) {
        Write-Error "World Server control endpoint returned HTTP $([int]$response.StatusCode). Body: $($response.Content). Stop-Process is an emergency/manual fallback only."
        exit 1
    }

    Write-Output "Graceful shutdown requested on control port $ControlPort."
    exit 0
}
catch {
    $errorResponse = $_.Exception.Response
    if ($null -ne $errorResponse) {
        $responseBody = ''
        try {
            $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Dispose()
        }
        catch {
            $responseBody = '<response body unavailable>'
        }

        Write-Error "World Server control endpoint returned HTTP $([int]$errorResponse.StatusCode). Body: $responseBody. Stop-Process is an emergency/manual fallback only."
    }
    else {
        Write-Error "Could not request graceful shutdown at $endpointUri within $TimeoutSeconds second(s): $($_.Exception.Message) Stop-Process is an emergency/manual fallback only."
    }

    exit 1
}
