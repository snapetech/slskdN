class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080501-slskdn.302"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080501-slskdn.302/slskdn-main-osx-arm64.zip"
      sha256 "c852ed6ea0c75c7c29861ee0f1ef356661dce30131e831116fa1965ea1b62097"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080501-slskdn.302/slskdn-main-osx-x64.zip"
      sha256 "4d1ec35d910d9a34285b5f4b2e26becfa69c210b6bf4394bb8f022116dfb1443"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080501-slskdn.302/slskdn-main-linux-glibc-x64.zip"
    sha256 "e9f727fc28be6e7ea8e1301223e57a20bd76c87b312d9da54be9dd98cb426375"
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
