class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080315-slskdn.296"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080315-slskdn.296/slskdn-main-osx-arm64.zip"
      sha256 "28d536ffc4f9c61707c65d31fdb5d454caee5f400442a1d82583ecdf5c25a0dc"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080315-slskdn.296/slskdn-main-osx-x64.zip"
      sha256 "3ddedc67f0f2bef11390c4b82d781b776e93141c9e31aa4e257e30822f73a91a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080315-slskdn.296/slskdn-main-linux-glibc-x64.zip"
    sha256 "9af9c16209844c13d532ff0dbbbe60ed6bb7e38f38cc0fa4d7b92a4b354c916a"
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
