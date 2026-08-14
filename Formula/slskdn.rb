class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081323-slskdn.304"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081323-slskdn.304/slskdn-main-osx-arm64.zip"
      sha256 "9997073ba11e36224f8c6697ba549ea864eddd39298358f3afef1cc71761b897"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081323-slskdn.304/slskdn-main-osx-x64.zip"
      sha256 "49a5c8242fb8601bff7429e50f80c84483339bca85d3972c2d3e942901092fc7"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081323-slskdn.304/slskdn-main-linux-glibc-x64.zip"
    sha256 "f017468d7f1525701ca9de7db4dcad3f6ed6f378e554b5c23c5803ff2d65c4b3"
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
