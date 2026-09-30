class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026093019-slskdn.332"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026093019-slskdn.332/slskdn-main-osx-arm64.zip"
      sha256 "af3d6bea0ff0cc6d801edcbfcd3a83e8305e3dcc319e95ab4787c0205425ea32"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026093019-slskdn.332/slskdn-main-osx-x64.zip"
      sha256 "98d1bd4e3ce877727e1f0ceaa3b3c43b7b26fa938190877fd81e6231a713151a"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026093019-slskdn.332/slskdn-main-linux-glibc-x64.zip"
    sha256 "5e1abb0e5009bfb444018f94fe3f72829e64c01c8a629931ede58a1046c6c468"
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
