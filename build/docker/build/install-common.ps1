Write-Host "Running script $PSCommandPath";

[System.IO.Directory]::CreateDirectory("C:\temp") | Out-Null;

Write-Host "Settings TLS 1.2 as default";

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; 

Write-Host "Removing CURL Powershell alias"

New-Item $profile -force -itemtype file | Out-Null

[System.IO.File]::WriteAllText($profile, "remove-item alias:curl") | Out-Null;

. $profile | Out-Null