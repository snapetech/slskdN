class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072016-slskdn.287"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072016-slskdn.287/slskdn-main-osx-arm64.zip"
      sha256 "a34876d9790271dbc62cb19686d5c6fd1068fc9a6a9f7e4ac09a1fd5875ca57f"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072016-slskdn.287/slskdn-main-osx-x64.zip"
      sha256 "edc61e7447c0f66dd6fc2bf6c5a0b507f65b8e37176f12c896619b3b102b7886"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072016-slskdn.287/slskdn-main-linux-glibc-x64.zip"
    sha256 "a28f4017efc1ec01143b184c694d9accfaf557cc11138e7ea82618619b9cdbe8"
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
