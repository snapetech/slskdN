class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100520-slskdn.344"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100520-slskdn.344/slskdn-main-osx-arm64.zip"
      sha256 "24661c28f866b529a1b7b66ae2f062d8ea6a72ff90256b36c7b8747c9e3c0207"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100520-slskdn.344/slskdn-main-osx-x64.zip"
      sha256 "02f91820b8b9a8bede091c67f94e45d9841b6ba7a7a83d609827b4aec1b1e05a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100520-slskdn.344/slskdn-main-linux-glibc-x64.zip"
    sha256 "fc6f7834625b2647a15348b3f22fe0cb2bb3b60eb79afbe96420c55119377934"
  end
  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
    if (libexec/"vpn-agent/slskdN-vpn-agent").exist?
      (bin/"slskdN-vpn-agent").write_exec_script libexec/"vpn-agent/slskdN-vpn-agent"
    end
  end
end
