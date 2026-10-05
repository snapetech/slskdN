class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100511-slskdn.341"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100511-slskdn.341/slskdn-main-osx-arm64.zip"
      sha256 "76f6a1099a13ac8b9c7a051e68ebb63df6eac424da5e48e2e89957ca8f2129f1"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100511-slskdn.341/slskdn-main-osx-x64.zip"
      sha256 "c94c61b24054c75023e9400f6db1fac82b162a03fea3a17fa7c3cfd5f50583ae"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100511-slskdn.341/slskdn-main-linux-glibc-x64.zip"
    sha256 "7591297934fd59034d0ff08bb2e3d50acddab05ad8791470deb0dc0c40f21695"
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
