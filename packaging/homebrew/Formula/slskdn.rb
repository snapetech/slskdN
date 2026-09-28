class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092818-slskdn.327"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092818-slskdn.327/slskdn-main-osx-arm64.zip"
      sha256 "980b496aa61085ce506f612b761fb513bf2297fc43466155d4f950264d3ec250"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092818-slskdn.327/slskdn-main-osx-x64.zip"
      sha256 "2f5b680e519ebfc7fa880ee1c79eb6dc0bb5a34d75d12ce75b26d472519d2027"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092818-slskdn.327/slskdn-main-linux-glibc-x64.zip"
    sha256 "f74494fef156c5c0177dfad591ef06b094449ca2b9232b483b4689af74621b6c"
  end

  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
    if (libexec/"vpn-agent/slskdN-vpn-agent").exist?
      (bin/"slskdN-vpn-agent").write_exec_script libexec/"vpn-agent/slskdN-vpn-agent"
    end
  end

  test do
    assert_match "slskd", shell_output("#{bin}/slskd --help", 1)
    if (bin/"slskdN-vpn-agent").exist?
      assert_match "slskdN-vpn-agent", shell_output("#{bin}/slskdN-vpn-agent --help")
    end
  end
end
