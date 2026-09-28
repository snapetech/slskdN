class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092816-slskdn.326"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092816-slskdn.326/slskdn-main-osx-arm64.zip"
      sha256 "8c10624baf4100ee67f43f252d2774fa4e0c3ea10267842afaaf59b03f37a9c3"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092816-slskdn.326/slskdn-main-osx-x64.zip"
      sha256 "83bde36cbda360f60f8bfd687cf32b2e621c05f979be1e916d498a00dab8be03"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092816-slskdn.326/slskdn-main-linux-glibc-x64.zip"
    sha256 "654cd3d06cd17f02a38674480c251c9b93e5deeacb421c4ae12c3282eb2e5a38"
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
