class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100521-slskdn.345"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100521-slskdn.345/slskdn-main-osx-arm64.zip"
      sha256 "1788c3fb45762fe49fc1c5e013085393b3eb6679d87285f5a979981e2bdd8f6a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100521-slskdn.345/slskdn-main-osx-x64.zip"
      sha256 "5887670fa9a74d356faca14051532973d162f3177444b2da3fe210be9c4e997d"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100521-slskdn.345/slskdn-main-linux-glibc-x64.zip"
    sha256 "abd1055c034b514754cd1dc4d722d1152d76f9aa5d5ad63d3273c69df262496d"
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
