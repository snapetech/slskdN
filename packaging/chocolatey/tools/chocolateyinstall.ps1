$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026100421-slskdn.338/slskdn-main-win-x64.zip"
$checksum   = "64c36487d06a6d27cdeee5c8b411ba45567990f8c7387b063dc8cb6a8be8cec7"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'

$vpnAgent = Join-Path $toolsDir "vpn-agent\slskdN-vpn-agent.exe"
if (Test-Path $vpnAgent) {
    Install-BinFile -Name "slskdN-vpn-agent" -Path $vpnAgent
}
