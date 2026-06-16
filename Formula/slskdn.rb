class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026061620-slskdn.271"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026061620-slskdn.271/slskdn-main-osx-arm64.zip"
      sha256 "6515d639a23c084e7c073dc1694f6085646f17daad983dc212441bb42d0d97bf"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026061620-slskdn.271/slskdn-main-osx-x64.zip"
      sha256 "33146fcbc7cdbbb732422cb51242518af3cb3431751d560fa214ae1ca73e27a6"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026061620-slskdn.271/slskdn-main-linux-glibc-x64.zip"
    sha256 "9080723dc4443c50db077ed227264fa1894b65f36bac5cae82bb282171a842e2"
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
