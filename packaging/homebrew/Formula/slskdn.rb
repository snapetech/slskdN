class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081400-slskdn.305"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081400-slskdn.305/slskdn-main-osx-arm64.zip"
      sha256 "bd77101faa48f82b9822f92a6902b797c5babeab9595b9ca796666cf76a7cea8"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081400-slskdn.305/slskdn-main-osx-x64.zip"
      sha256 "2653be08573445098914968c453b90d8481c40f388ec39f5af6cba96a449e2c8"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081400-slskdn.305/slskdn-main-linux-glibc-x64.zip"
    sha256 "7c2b671cd96b0e559e03a046b7eb84eed258ea1b14fa947e907abafe62ceb6d7"
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
