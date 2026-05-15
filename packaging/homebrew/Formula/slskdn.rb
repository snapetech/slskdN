class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051509-slskdn.255"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.255/slskdn-main-osx-arm64.zip"
      sha256 "f633929fd12ac624b028b7cf21d35fb3e9ee9f29207dfcc63c8f24afa9961941"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.255/slskdn-main-osx-x64.zip"
      sha256 "949c9a6abfb82c106f62f035ee148a30411106104655d29119cdbb66170acd60"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.255/slskdn-main-linux-glibc-x64.zip"
    sha256 "4d0ffc0e4b8cc570cc5a15e150a60ce84b2290ad99a477076747885c0eadb378"
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
