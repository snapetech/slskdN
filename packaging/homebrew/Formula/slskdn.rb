class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071501-slskdn.276"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071501-slskdn.276/slskdn-main-osx-arm64.zip"
      sha256 "44b5f251b673c8354bf76bec67f310c1feff6ba58209b29cc4eedf9768f293bd"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071501-slskdn.276/slskdn-main-osx-x64.zip"
      sha256 "b19c7d8b336bab9c3882a914c987bc0192172ee1572bcd56b566102fa108729d"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071501-slskdn.276/slskdn-main-linux-glibc-x64.zip"
    sha256 "31b17fdf8e69f100cdbc921f473175dc96a840fef716ea3eb4c781cba6504c56"
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
