class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026082519-slskdn.317"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026082519-slskdn.317/slskdn-main-osx-arm64.zip"
      sha256 "a5aacbbf8bac1c753261262cf887ec245f2b2aac97954b015cde005738a99f29"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026082519-slskdn.317/slskdn-main-osx-x64.zip"
      sha256 "470d624d3bba170d1528cee0309873c2afca6e601ba123ba3e7cb0377aa51018"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026082519-slskdn.317/slskdn-main-linux-glibc-x64.zip"
    sha256 "7b3c070c334e1a6498058fd3485619f13218ce2440056183999407947686ea87"
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
