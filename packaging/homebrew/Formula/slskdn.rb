class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081719-slskdn.309"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.309/slskdn-main-osx-arm64.zip"
      sha256 "40b60bda1fc2757b911d35f8d1719fb2769a9db44138c8652d719976d6597822"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.309/slskdn-main-osx-x64.zip"
      sha256 "ff79cce5a2864d5a8d828b988befe9ee0dcfa6a17ea0843c44180091a0a13c1f"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.309/slskdn-main-linux-glibc-x64.zip"
    sha256 "23c5c230a639af01ed1c49df961184879cf182f9fe8261bcfcdf96e093370f4a"
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
