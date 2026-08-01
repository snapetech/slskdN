class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080114-slskdn.294"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080114-slskdn.294/slskdn-main-osx-arm64.zip"
      sha256 "1c6007d747fa47e56d2ce3e1cc58bd5b44dd2042706dd841c68d638ff133bfdc"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080114-slskdn.294/slskdn-main-osx-x64.zip"
      sha256 "dee677af4effa5199417b13d44b5c79199c9f7275d5c91edd5e7c41a10b2925e"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080114-slskdn.294/slskdn-main-linux-glibc-x64.zip"
    sha256 "4c4defd9cf7d181664778d0ed76bcddb4be3b5248414011990b1626de4e2f970"
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
