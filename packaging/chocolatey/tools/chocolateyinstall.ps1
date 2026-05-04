$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050300-slskdn.219/slskdn-main-win-x64.zip"
$checksum   = "0c6d50e22aef7e4d196677b0a695d560d86a2b6f8755b4ddd34a71e56e49af51"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
