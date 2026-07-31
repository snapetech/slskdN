class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026073117-slskdn.293"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026073117-slskdn.293/slskdn-main-osx-arm64.zip"
      sha256 "348c29ae399519f6c2312bac2946f2b9fe8c35cd78af4dfcc1327d3ea95a3fdd"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026073117-slskdn.293/slskdn-main-osx-x64.zip"
      sha256 "dcc93d432067d785774fe5517e0a7f62caffe4f8b0af1d599d2f03ff22a98a35"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026073117-slskdn.293/slskdn-main-linux-glibc-x64.zip"
    sha256 "3a0bc35129a2549db302f7cb64996ae2b5e148790ff14ec1356cd68105e568ba"
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
