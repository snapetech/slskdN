$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050600-slskdn.227/slskdn-main-win-x64.zip"
$checksum   = "58910171b49371741b9e2698badd228df0bf06bf90d69c95d923614565d56796"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
