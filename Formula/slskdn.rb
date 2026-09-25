class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092516-slskdn.324"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092516-slskdn.324/slskdn-main-osx-arm64.zip"
      sha256 "100bb90791dd2a6b611046cb26fbd2fcb8ad12250b0561ffd6fdfdbcdb21e742"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092516-slskdn.324/slskdn-main-osx-x64.zip"
      sha256 "068789d095fc3675dbc76ecd5bc58384728fe21ed5d7835cd63fde2eb378ed3a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092516-slskdn.324/slskdn-main-linux-glibc-x64.zip"
    sha256 "c318f32a1d5afa5665a012098c00b882426e4214e487224c291ca6286058f30d"
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
