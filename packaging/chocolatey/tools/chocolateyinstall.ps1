$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026090620-slskdn.321/slskdn-main-win-x64.zip"
$checksum   = "1c4cc61b3668d57a9dedff65a79e96b1b4e9a82b0ecd464c016b3bbf9003ccce"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'

$vpnAgent = Join-Path $toolsDir "vpn-agent\slskdN-vpn-agent.exe"
if (Test-Path $vpnAgent) {
    Install-BinFile -Name "slskdN-vpn-agent" -Path $vpnAgent
}
