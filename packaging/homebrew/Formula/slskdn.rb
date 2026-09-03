class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026090321-slskdn.320"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026090321-slskdn.320/slskdn-main-osx-arm64.zip"
      sha256 "3f0fe4f90b1a563fe82e87074398f3c197b91095305dc8d01352f874b2288e65"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026090321-slskdn.320/slskdn-main-osx-x64.zip"
      sha256 "f1f52d60c7ee1e1d4638db7236eaca888c3046e1a4dbd67a2844f6eb13c23982"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026090321-slskdn.320/slskdn-main-linux-glibc-x64.zip"
    sha256 "32cfa00d4c8de04611507f1ae8623aad46c7ed38f2807c08cb11991e15aa2170"
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
