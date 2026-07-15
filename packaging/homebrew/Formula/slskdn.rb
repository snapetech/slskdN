class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071502-slskdn.277"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071502-slskdn.277/slskdn-main-osx-arm64.zip"
      sha256 "c0660eb0a77786056223fdf2a37fda3320273ce86394a71f9cb9d5acaef9c991"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071502-slskdn.277/slskdn-main-osx-x64.zip"
      sha256 "69c288a4b9fd4ff96767f3025d0e8add2e78ec57ef9d9e5adb525020690bdc35"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071502-slskdn.277/slskdn-main-linux-glibc-x64.zip"
    sha256 "35646c75bcd431e5cabf664c2f08e5adfa5815292fc01a6a1d35d6cc8b681971"
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
