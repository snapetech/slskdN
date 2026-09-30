class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026093001-slskdn.330"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026093001-slskdn.330/slskdn-main-osx-arm64.zip"
      sha256 "587e5630a32f24d2df90b0089b8781c0bb82c8f6631cd1e1542b87f1dfb66179"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026093001-slskdn.330/slskdn-main-osx-x64.zip"
      sha256 "6bc0a883432aa5eda3c32e82257cfb0adeb7c16a17ffcb910f6643ed8e2e4e0a"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026093001-slskdn.330/slskdn-main-linux-glibc-x64.zip"
    sha256 "0f0dad54bed23dcc59fc3cbe45944b0508178f3e8bf36ffc0132f3d07bd322f9"
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
