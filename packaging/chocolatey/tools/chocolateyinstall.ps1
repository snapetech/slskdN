$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026051217-slskdn.240/slskdn-main-win-x64.zip"
$checksum   = "3e973a1415755b59908a7b336cb7e9ba11b3d0b313c71e805e89f6b94878d65b"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'

$vpnAgent = Join-Path $toolsDir "vpn-agent\slskdN-vpn-agent.exe"
if (Test-Path $vpnAgent) {
    Install-BinFile -Name "slskdN-vpn-agent" -Path $vpnAgent
}
