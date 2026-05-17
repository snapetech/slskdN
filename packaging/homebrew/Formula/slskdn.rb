class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051718-slskdn.262"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.262/slskdn-main-osx-arm64.zip"
      sha256 "05083cea6b34011d5c478b91694d5894a3f358c3a6a01bf1c5deb3c8b7465999"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.262/slskdn-main-osx-x64.zip"
      sha256 "6b495399e9a7f68ca0a36a026441d0ab183e3eab5ffc410a604174ff77992d3d"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051718-slskdn.262/slskdn-main-linux-glibc-x64.zip"
    sha256 "8380a12c7c4a41de2d2598d44e08dcdf4ec0dcf393aab877ac1f358667516d5c"
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
