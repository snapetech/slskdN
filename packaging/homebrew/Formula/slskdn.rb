class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072717-slskdn.292"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072717-slskdn.292/slskdn-main-osx-arm64.zip"
      sha256 "4a4a47b1c35fbebf0b4748ee9cb5d1df313f6ad22d2351c339f8cb61fae237cc"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072717-slskdn.292/slskdn-main-osx-x64.zip"
      sha256 "a334bf01e64051159589258ec9839e6e3a3bc0c1913d8a46c9b2c824d26a660f"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072717-slskdn.292/slskdn-main-linux-glibc-x64.zip"
    sha256 "5cd162021cdb5b0515d09da0fb55a986dd47a5e30cb0b43187f0d414586517e7"
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
