class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072418-slskdn.290"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072418-slskdn.290/slskdn-main-osx-arm64.zip"
      sha256 "0c096ae8b78f2238e355d77ac2bf1c00cfcf38c5fe4af7702fcc1f345223f52a"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072418-slskdn.290/slskdn-main-osx-x64.zip"
      sha256 "7961af5cb09d7cbe2bec2e65542d61bc7b185c7dacd2018b00c067a8e84277d6"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072418-slskdn.290/slskdn-main-linux-glibc-x64.zip"
    sha256 "f855522bbb0b9111f1154bbacba58aa45def44779f3476ae412720f7aa5f698b"
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
