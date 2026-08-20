class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026082016-slskdn.315"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026082016-slskdn.315/slskdn-main-osx-arm64.zip"
      sha256 "bb51351d9fc66f77b7e173e6c0438369f025e173f79943baa5d531dc54c175d2"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026082016-slskdn.315/slskdn-main-osx-x64.zip"
      sha256 "ebc5fa78d8129e8447e0b299e29ebfef419179b5bd5a2139b8d2ad88b8700011"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026082016-slskdn.315/slskdn-main-linux-glibc-x64.zip"
    sha256 "9fab8cc17cd859f6a5c5749cf0840cfe0bab77b933c058eeff5b0190c2ae667f"
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
