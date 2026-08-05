class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080512-slskdn.303"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080512-slskdn.303/slskdn-main-osx-arm64.zip"
      sha256 "eca1bf5cb86da25e60179d456fb4445605c9d832afdf9da24712d794b552d0eb"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080512-slskdn.303/slskdn-main-osx-x64.zip"
      sha256 "78c7d8c8791cc8ac63bcdcf82180442a6b1841e762d86d8bc1fc6ccdcaec1063"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080512-slskdn.303/slskdn-main-linux-glibc-x64.zip"
    sha256 "d1f1d8e0eb0720b13b9559c803e2e3e4615be5a201ba7450b2847046237d1994"
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
