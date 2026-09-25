$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026092517-slskdn.325/slskdn-main-win-x64.zip"
$checksum   = "4f58643552d7024ae401b961022304a4941ddbc0000a04b1200cc7864e92cb88"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'

$vpnAgent = Join-Path $toolsDir "vpn-agent\slskdN-vpn-agent.exe"
if (Test-Path $vpnAgent) {
    Install-BinFile -Name "slskdN-vpn-agent" -Path $vpnAgent
}
