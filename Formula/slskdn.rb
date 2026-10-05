class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100505-slskdn.340"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100505-slskdn.340/slskdn-main-osx-arm64.zip"
      sha256 "94643094559dceae8b763c19b6120abe300d38613e13b9b5e612d4cb35f0c5d5"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100505-slskdn.340/slskdn-main-osx-x64.zip"
      sha256 "229101b086bc8c654d050e413b1e5b8a5458c7c8ad78ede6b17fc0936ec37640"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100505-slskdn.340/slskdn-main-linux-glibc-x64.zip"
    sha256 "855d49abeb66a8e75ff0344587fea221cd409c06d36fbe250023b6d3da2c681b"
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
