class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071723-slskdn.284"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071723-slskdn.284/slskdn-main-osx-arm64.zip"
      sha256 "848cef9f51fe5f064fdfcb3e27331d669a08e23e6c1d2762c2297203a8529aea"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071723-slskdn.284/slskdn-main-osx-x64.zip"
      sha256 "8a1c0a86ec79d28e467a56c6611fe092fdb546466d44313ff8967bca35f4781e"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071723-slskdn.284/slskdn-main-linux-glibc-x64.zip"
    sha256 "ba1f73a1a304c38cf09e6a8b892138b6e32d8640b5f814a536b3738ee644b1f9"
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
