class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052520-slskdn.270"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052520-slskdn.270/slskdn-main-osx-arm64.zip"
      sha256 "fe9a2c03053db9a7fd9e35ea577cdb76cd16920d775461b48358a8c428f9a86d"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052520-slskdn.270/slskdn-main-osx-x64.zip"
      sha256 "4d1d0b9a0caea149483e41f3e0cf9120c3fe967c0179ede2a1b440ab85f6d76c"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052520-slskdn.270/slskdn-main-linux-glibc-x64.zip"
    sha256 "c24a435decc5862dcb1bdadf3dfd06b073568eb19624219a878dd8448304010a"
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
