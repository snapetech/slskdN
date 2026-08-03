class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080320-slskdn.298"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080320-slskdn.298/slskdn-main-osx-arm64.zip"
      sha256 "67fc15b255cb2b5ef12695648e3775ca12d3dfe467e9f11adc9de13271612f80"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080320-slskdn.298/slskdn-main-osx-x64.zip"
      sha256 "4b2c04d6f110d6182d5c5cffb3922f4b8c252e3cf99013b6baa61095b45dd3db"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080320-slskdn.298/slskdn-main-linux-glibc-x64.zip"
    sha256 "361ab1c713fb24348401667eedb2bff86696227f65de18d7880f1f4be91c9e64"
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
