class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072416-slskdn.288"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072416-slskdn.288/slskdn-main-osx-arm64.zip"
      sha256 "98d573aac72646c9799c3f154a9fc24fb8cae90832f4840d5ec28bb138f926d1"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072416-slskdn.288/slskdn-main-osx-x64.zip"
      sha256 "d8bc43eccbd3fba8365909365f7adda59f8a93ecc84d7def34d451225d12365b"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072416-slskdn.288/slskdn-main-linux-glibc-x64.zip"
    sha256 "74858fc8a30d0cc80a5aaa7559f17dbc089539d763b26f7130df55d37b814e5f"
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
