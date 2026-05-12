class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051219-slskdn.243"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051219-slskdn.243/slskdn-main-osx-arm64.zip"
      sha256 "c1b17ad200390db803cbd96bb3b9e3a03b21f1b74efe2b17f3d0f48e9cf0f7f9"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051219-slskdn.243/slskdn-main-osx-x64.zip"
      sha256 "1f89d5baa59f7faf83363f07fcf45bb4cca177f137040d47bf09890e4f783113"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051219-slskdn.243/slskdn-main-linux-glibc-x64.zip"
    sha256 "932423592713ce2cae9ee0e5d76d62858da1e4e493cf4c8c9ba4be671fe29cfb"
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
