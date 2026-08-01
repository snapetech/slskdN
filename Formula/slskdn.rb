class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080115-slskdn.295"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080115-slskdn.295/slskdn-main-osx-arm64.zip"
      sha256 "3a92a28110ce08976675827689dbae7c0f165692dad25caf77c8669bb936b245"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080115-slskdn.295/slskdn-main-osx-x64.zip"
      sha256 "a7ea0ce92a6f6f6ac1b6238965d2b69c56192958a8a7a0720d7e21b97cfcc6e9"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080115-slskdn.295/slskdn-main-linux-glibc-x64.zip"
    sha256 "32c3d87fd215b403970d9345bc4ad5d829ac8b9e26f5738618f92e9e710c6d0c"
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
