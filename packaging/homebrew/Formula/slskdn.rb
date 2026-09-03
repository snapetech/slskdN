class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026090318-slskdn.318"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.318/slskdn-main-osx-arm64.zip"
      sha256 "04d6c0546de1cdf18af279b14863deb9b692d878e79053a3d0e76b3111c5f04f"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.318/slskdn-main-osx-x64.zip"
      sha256 "e40bf9c0d05d72008fdeb5d26a020d2dba3e99bf08cb83964659dded353c0ea2"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.318/slskdn-main-linux-glibc-x64.zip"
    sha256 "29d23f590c6696f2770d2820f2cf9d05320b8209becf2a86dc1f49f8b1bf3617"
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
