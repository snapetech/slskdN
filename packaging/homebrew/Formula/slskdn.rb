class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051221-slskdn.247"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051221-slskdn.247/slskdn-main-osx-arm64.zip"
      sha256 "2af080a7f99e65c6f460b3e4a95885bb901228a0ef162104973008071b16587a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051221-slskdn.247/slskdn-main-osx-x64.zip"
      sha256 "1e350b5b6d8cddf224d6cd904b2d9a2a1d1b361b95abf80c713448389d605382"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051221-slskdn.247/slskdn-main-linux-glibc-x64.zip"
    sha256 "fb4480b02ca17dd4f6bacc9728c16cf3700b6a317a6b174138d5e8c72255cb5d"
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
