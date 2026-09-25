class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092517-slskdn.325"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092517-slskdn.325/slskdn-main-osx-arm64.zip"
      sha256 "27901ec6325a8c63dbf6f076a9c87eab97e62a6d14c25ddc7f19089e66dbc274"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092517-slskdn.325/slskdn-main-osx-x64.zip"
      sha256 "3d576a6675376cfe848c5bea0a9d12a63903ddaa5782d7c02bceb803caf40ef6"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092517-slskdn.325/slskdn-main-linux-glibc-x64.zip"
    sha256 "31c929d2755beb6c058454e65b8d08b1c080d6bfc713c5a973e7a943236740a7"
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
