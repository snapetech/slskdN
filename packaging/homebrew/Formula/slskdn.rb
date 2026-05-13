class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051318-slskdn.250"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051318-slskdn.250/slskdn-main-osx-arm64.zip"
      sha256 "cdc0ee34e0edc84237924d627d2856dd4c6b441d215422635098d1b16ed31c3a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051318-slskdn.250/slskdn-main-osx-x64.zip"
      sha256 "3a0ada3f4906409fc24a921fefb38f11496bf68b0c656da5686e67b6c88e5bc7"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051318-slskdn.250/slskdn-main-linux-glibc-x64.zip"
    sha256 "ed2d793d0606dea4ebd003089b62f762399b4b3b590a4c16853be2840bbe5bf7"
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
