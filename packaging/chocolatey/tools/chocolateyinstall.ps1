$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.224/slskdn-main-win-x64.zip"
$checksum   = "4370cd218aecec25d2d6e8bb727aea0380e418656b38e1d12f4a89397f11b72e"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
