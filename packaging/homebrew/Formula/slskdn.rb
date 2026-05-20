class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052023-slskdn.265"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052023-slskdn.265/slskdn-main-osx-arm64.zip"
      sha256 "8f7a45ea094210cc9f788a0331fbbb350d6f606f0850c28d58d82ebc57f6f14d"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052023-slskdn.265/slskdn-main-osx-x64.zip"
      sha256 "eec8bad5563d5bb9cfbcbf6982d7d94b87183efeefaf81a62c865d63a0da062a"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052023-slskdn.265/slskdn-main-linux-glibc-x64.zip"
    sha256 "dd710155c637c4a783b260bf0e64b1b956fe7d7c3f30b207ea500ec51716dd6e"
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
