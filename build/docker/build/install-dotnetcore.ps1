$sdk_lts_url = "https://download.visualstudio.microsoft.com/download/pr/5954c748-86a1-4823-9e7d-d35f6039317a/169e82cbf6fdeb678c5558c5d0a83834/windowsdesktop-runtime-3.1.3-win-x64.exe";
Write-Host "Downloading .NET Core LTS $sdk_lts_url";
$dotnetsdk_lts = "C:\temp\netsdk_lts.exe";
& curl -o $dotnetsdk_lts -L $sdk_lts_url | Write-Host;
Write-Host "Installing .NET Core SDK LTS";
& $dotnetsdk_lts -q | Write-Host;
Write-Host ".NET Core LTS installation exit code was $lastexitcode";

if ($lastexitcode -ne 0) {
    EXIT 1;
}

Exit $lastexitcode;