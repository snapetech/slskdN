class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026061721-slskdn.273"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026061721-slskdn.273/slskdn-main-osx-arm64.zip"
      sha256 "411ab6c6c6e9952128085b089b6e31c1a555470de4215d61e7d9f853c0f7b89c"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026061721-slskdn.273/slskdn-main-osx-x64.zip"
      sha256 "de23e067faf0c1b84e0f310e43d4e61ac0b81d102ca533f719eb1ac065e55f8f"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026061721-slskdn.273/slskdn-main-linux-glibc-x64.zip"
    sha256 "e655f0a2d35123f649801ce8ea6aab2c1f2624cfb21dfed951fa2e88de126198"
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
