class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026090318-slskdn.319"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.319/slskdn-main-osx-arm64.zip"
      sha256 "c8e5a62366062c24c8e31d6bbe1498f1584a0ca34bf189a2d92ddee97cd74e9b"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.319/slskdn-main-osx-x64.zip"
      sha256 "1cb426f9a60c04327a290125d17243317d01c42fb851b06da628fd96aec0d99c"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026090318-slskdn.319/slskdn-main-linux-glibc-x64.zip"
    sha256 "ca8f062a0921a4222db25d3b036c149e5d4c083dcdd68d2e0d5bd76811ba4444"
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
