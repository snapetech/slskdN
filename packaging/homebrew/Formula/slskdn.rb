class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026090620-slskdn.321"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026090620-slskdn.321/slskdn-main-osx-arm64.zip"
      sha256 "141e22a117319f4c88e435ae70a688d5c3b65f6c385d6b1042fe1f688ead0289"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026090620-slskdn.321/slskdn-main-osx-x64.zip"
      sha256 "ffa94d00c8e96e46ac40fa0961100cf3faf337c8bd6f311202043c09527b928d"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026090620-slskdn.321/slskdn-main-linux-glibc-x64.zip"
    sha256 "0c7fb4e54bfbfef4cebee98ec7eb653e092039f35eb373e90dc1f8d2f6682305"
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
