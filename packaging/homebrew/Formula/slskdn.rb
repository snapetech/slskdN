class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081719-slskdn.310"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.310/slskdn-main-osx-arm64.zip"
      sha256 "56397bc5ce609432666a2721ee8338ee61dadb3b81df3758e055087ffa60717e"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.310/slskdn-main-osx-x64.zip"
      sha256 "5d6a06f73f0fc9d666ced2016928b08a35dff260ef0581d4059699f9d4418034"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081719-slskdn.310/slskdn-main-linux-glibc-x64.zip"
    sha256 "00ee73f97844606abf5b53d1693c9e6b07cbefa7b28e2f57347b95565229b902"
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
