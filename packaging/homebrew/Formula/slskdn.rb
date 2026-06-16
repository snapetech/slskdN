class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026061621-slskdn.272"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026061621-slskdn.272/slskdn-main-osx-arm64.zip"
      sha256 "cf9032d90b090d7da49fa884a6fb26842a70115507a590c4916a3e5b060e2950"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026061621-slskdn.272/slskdn-main-osx-x64.zip"
      sha256 "2d476fbe2d4b0d6e2ad53b891e10d9ad62ec2b79b71090ddf42db070e78e851b"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026061621-slskdn.272/slskdn-main-linux-glibc-x64.zip"
    sha256 "5f85b00cb1bee1484743b0ceef27b2013835505b2114ba30af8eef5e7a4b7ec0"
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
