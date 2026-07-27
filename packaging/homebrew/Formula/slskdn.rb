class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072716-slskdn.291"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072716-slskdn.291/slskdn-main-osx-arm64.zip"
      sha256 "5d919e2d2b56add66a85a3e4535b7c4a2b6ddc5be8a994b81dc3b9340b25cf52"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072716-slskdn.291/slskdn-main-osx-x64.zip"
      sha256 "1d640048acd2f6c490fea2f0b92e993d2e8740a3093441b94d1d32a193f66537"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072716-slskdn.291/slskdn-main-linux-glibc-x64.zip"
    sha256 "7c7b0b07bd8e5523fdf8740811d16872fc2d8c7baafe3f549cf1cf94da054352"
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
