class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100514-slskdn.342"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100514-slskdn.342/slskdn-main-osx-arm64.zip"
      sha256 "f6f2fb7b49cc294c5ccda3f3dde45f1662d53c32ca8650c62adc8494d74157e5"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100514-slskdn.342/slskdn-main-osx-x64.zip"
      sha256 "d32bc11e54c02166b249775fd3a65cff0206368178db47950c87b3c323384683"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100514-slskdn.342/slskdn-main-linux-glibc-x64.zip"
    sha256 "10da268521b8c680d50e5ab0f7f7fa48fe0712e54770dda8cf8df5893b94c6ff"
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
