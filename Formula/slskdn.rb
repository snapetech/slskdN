class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051519-slskdn.257"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051519-slskdn.257/slskdn-main-osx-arm64.zip"
      sha256 "40e8fd4ac4cb84bb1b49abcbbd7f450acfc556907f066451f80c952968ecbf11"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051519-slskdn.257/slskdn-main-osx-x64.zip"
      sha256 "41c7fe17516ccfd5f9c912f613ea24c0e9dd2b08def282becad4cda6f592c39d"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051519-slskdn.257/slskdn-main-linux-glibc-x64.zip"
    sha256 "c41d633fa720ee3f1b57599d3725db7b541fd7e24380827b46e2d76c25b7bf70"
  end
  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
    if (libexec/"vpn-agent/slskdN-vpn-agent").exist?
      (bin/"slskdN-vpn-agent").write_exec_script libexec/"vpn-agent/slskdN-vpn-agent"
    end
  end
end
