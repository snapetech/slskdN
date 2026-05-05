$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.223/slskdn-main-win-x64.zip"
$checksum   = "7080e0a5e9c0a8016ddaf32dcb1cc8a5d4d3813e76136d7a06b86f0c44c37ced"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
