class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052503-slskdn.269"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052503-slskdn.269/slskdn-main-osx-arm64.zip"
      sha256 "59a792bfb670622a67d8dc0b1e80b4e27006dad14f3822646d5c958f4917b205"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052503-slskdn.269/slskdn-main-osx-x64.zip"
      sha256 "d3f6495fcbc403f59e6b48b479bf04893d317bc78b9c5eae4e30605136bb506f"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052503-slskdn.269/slskdn-main-linux-glibc-x64.zip"
    sha256 "4d237b35f8e66df9ea39bfdb857a959780f4f96c4848c16e0b05a85803413bbd"
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
