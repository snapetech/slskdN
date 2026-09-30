class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026093022-slskdn.333"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026093022-slskdn.333/slskdn-main-osx-arm64.zip"
      sha256 "437e95d4dc429b046eabe0ea2187f41fa4457d2efa2ca378fe7d99b6509b165b"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026093022-slskdn.333/slskdn-main-osx-x64.zip"
      sha256 "9f8f00176ef41c3472ff231661a5f16fba5049fe6a3e36debcd70ce246ff7a1f"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026093022-slskdn.333/slskdn-main-linux-glibc-x64.zip"
    sha256 "114b2d8b1c389bf7cc0ff45f61d7b3d71de8bac1e5908ee36be769d6f5f84e1c"
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
