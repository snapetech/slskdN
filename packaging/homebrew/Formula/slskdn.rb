class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051904-slskdn.263"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051904-slskdn.263/slskdn-main-osx-arm64.zip"
      sha256 "f3581fd5246a09df3e3c5686986416219d781a1cec8742f1b2a7e3f3afcab03c"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051904-slskdn.263/slskdn-main-osx-x64.zip"
      sha256 "b5d69e70c259aadf038fd5d2516de1511613a00032cc13fadf4f9a821d1b5519"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051904-slskdn.263/slskdn-main-linux-glibc-x64.zip"
    sha256 "d562f3e647be25a0969e922249f94893c2c28f8fdfd87d34aa069c7668392544"
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
