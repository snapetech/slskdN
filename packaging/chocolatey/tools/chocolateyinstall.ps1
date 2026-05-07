$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/snapetech/slskdn/releases/download/2026050712-slskdn.232/slskdn-main-win-x64.zip"
$checksum   = "fece3c85299ae8406c6a786e6c86fbca8d8e55db35e4bb927bb244715280332b"

Install-ChocolateyZipPackage -PackageName 'slskdn' -Url $url -UnzipLocation $toolsDir -Checksum $checksum -ChecksumType 'sha256'
