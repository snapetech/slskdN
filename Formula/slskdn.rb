class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052014-slskdn.264"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052014-slskdn.264/slskdn-main-osx-arm64.zip"
      sha256 "2bd6b7d0b4e9aa68ffa206d88893f7df9a96febab6c407fecf202895eb4a35a5"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052014-slskdn.264/slskdn-main-osx-x64.zip"
      sha256 "963e679aef5babfa2cb11c76206f227b356fd3889591ac7f48d694af45dcd259"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052014-slskdn.264/slskdn-main-linux-glibc-x64.zip"
    sha256 "da31d8d46217c409cf3dabd5d94b5586e0b67c3d2c23385483d00195ac81c501"
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
