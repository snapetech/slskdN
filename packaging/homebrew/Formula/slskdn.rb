class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026093002-slskdn.331"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026093002-slskdn.331/slskdn-main-osx-arm64.zip"
      sha256 "d2c76bc93658fa1f7d9189c7f4ba1dde1a84897db1b95730b0c66c700733c87a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026093002-slskdn.331/slskdn-main-osx-x64.zip"
      sha256 "063e4439a4ce9192cfa825c31a54fdf7c03dd42405406167ee807770a08c0aa8"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026093002-slskdn.331/slskdn-main-linux-glibc-x64.zip"
    sha256 "22bfead6c9661aabc0187fda8c20e4fac2faf2d2faaea6a6316df9524868c735"
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
