class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081815-slskdn.312"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081815-slskdn.312/slskdn-main-osx-arm64.zip"
      sha256 "c3fc4018954f1216a27148f9961282a4e8058745eb62882daed12aea9fe5e7df"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081815-slskdn.312/slskdn-main-osx-x64.zip"
      sha256 "067b9c593ca4e77add50019593f99777ba64a273685f9382b55b1d1683647201"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081815-slskdn.312/slskdn-main-linux-glibc-x64.zip"
    sha256 "9b8eb9745339c324a980c2485bc662447e1bffb16de7d7931005b1a5e7532d63"
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
