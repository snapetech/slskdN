class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100314-slskdn.335"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100314-slskdn.335/slskdn-main-osx-arm64.zip"
      sha256 "46c456dbc25939b13d3f093848ed9efa14c806d0d88cc2192d5e1f4fce5f7c46"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100314-slskdn.335/slskdn-main-osx-x64.zip"
      sha256 "b5fcea2a26c44b3acdf28d40c13a9c6f1c0f4585dbb1caad11b98f0a83f29679"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100314-slskdn.335/slskdn-main-linux-glibc-x64.zip"
    sha256 "97bcae1fabff8b510b55b20b277d15e5bf0e8e7f5ee7d361cec6a541e6124371"
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
