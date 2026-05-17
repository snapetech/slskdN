class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051718-slskdn.261"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.261/slskdn-main-osx-arm64.zip"
      sha256 "69a4bf918e1bda14bf423bb92e796e357cb8fde3119a9f2910d40d2353f714c0"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.261/slskdn-main-osx-x64.zip"
      sha256 "e67162cfc4aa2d43f1780e82c5045a788b9cb68b62126ff6ec9a1e9788cb8f99"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.261/slskdn-main-linux-glibc-x64.zip"
    sha256 "be606e8b64b283fe4223dd16260891fa24788fa0db6b7f6648ace249118a1dd6"
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
