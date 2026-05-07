$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050720-slskdn.233/slskdn-main-win-x64.zip"
$checksum   = "75fbbc16bcfb0c6983ab28741c9d1dfaf11b981ddbf7b05d3b1aa0e9b7a646c5"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
