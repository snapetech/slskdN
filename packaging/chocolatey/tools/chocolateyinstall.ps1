$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050400-slskdn.220/slskdn-main-win-x64.zip"
$checksum   = "20045b3a0bd1ad5f0c8d4f2cdb8a445d9f10ad79071462ad3ee13de816725808"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
