class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026082115-slskdn.316"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026082115-slskdn.316/slskdn-main-osx-arm64.zip"
      sha256 "3a80ed45c10a8c4440ce1327fb55c193ed3fd84fa2922d9c1a777ca6fb42db61"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026082115-slskdn.316/slskdn-main-osx-x64.zip"
      sha256 "39c51479a18dfc59411e060f900048c37136b2b6761e093afb68f920c8c95247"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026082115-slskdn.316/slskdn-main-linux-glibc-x64.zip"
    sha256 "0a1b05ad7eaef84b1fa3c1f2aa78786e1a8e3daab3d3bae051e1a7b701656cd3"
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
