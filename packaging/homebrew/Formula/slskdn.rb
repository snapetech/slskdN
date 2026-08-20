class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026082015-slskdn.314"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026082015-slskdn.314/slskdn-main-osx-arm64.zip"
      sha256 "67cf87ac10de72b273cfd77c41d600d9316c6ef69cc062aaf77b0784d14f520a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026082015-slskdn.314/slskdn-main-osx-x64.zip"
      sha256 "c75bf8d51098b348933140e50cf73972232a1e86feb801cfcca0e559c37ee27c"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026082015-slskdn.314/slskdn-main-linux-glibc-x64.zip"
    sha256 "95a15d706fb0008defe8da5c631a3aea6ac96bf70885615a4f39ee3a03565261"
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
