class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071415-slskdn.274"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071415-slskdn.274/slskdn-main-osx-arm64.zip"
      sha256 "d4c35d823bbba22021038d34ca4cb17190178c672263647be65cd8cf0b95e84f"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071415-slskdn.274/slskdn-main-osx-x64.zip"
      sha256 "0cbc526e7cb13050d7d66fd5b1aadc7caca15bed8afb699f518401808f9a95a1"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071415-slskdn.274/slskdn-main-linux-glibc-x64.zip"
    sha256 "e3460ef8d9826002c453073db345637b2d10c0467cdab192cc09b159544ef938"
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
