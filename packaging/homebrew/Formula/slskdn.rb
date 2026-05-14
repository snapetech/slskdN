class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051416-slskdn.252"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051416-slskdn.252/slskdn-main-osx-arm64.zip"
      sha256 "347713f8518db573be08a4a0540c492c2752f1678b0f664a36c28a2bf7bea8bd"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051416-slskdn.252/slskdn-main-osx-x64.zip"
      sha256 "c41802a23d7d8e66860177bcf683a7ab555ce9298912bd008299ee754a1fd172"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051416-slskdn.252/slskdn-main-linux-glibc-x64.zip"
    sha256 "f37d9d1a91eca5f69c6eaafb1d9ef9e6e035190d1bf1aa1221aafb4dc549c80e"
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
