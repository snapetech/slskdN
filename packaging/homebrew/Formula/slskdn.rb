class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092900-slskdn.329"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092900-slskdn.329/slskdn-main-osx-arm64.zip"
      sha256 "20f64440b768f829d0264e7a429045aae779e7d5dcdb12b118aa604948b93fdb"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092900-slskdn.329/slskdn-main-osx-x64.zip"
      sha256 "a71058909eda54f7df14a0f440433aa8a11472222205b83da4ea32b7cf0a4139"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092900-slskdn.329/slskdn-main-linux-glibc-x64.zip"
    sha256 "54f39f1cdd83a44928bab2b9154f88bcc8ea74afeb64a21cef89bdc62508304e"
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
