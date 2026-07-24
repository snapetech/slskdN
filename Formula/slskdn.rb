class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072417-slskdn.289"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072417-slskdn.289/slskdn-main-osx-arm64.zip"
      sha256 "31b4e9cee62b55ed2ca453d4bc5dc81e5d0320aa25eeedcef3926dcd22930ade"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072417-slskdn.289/slskdn-main-osx-x64.zip"
      sha256 "3b56fc34a26ed907af78d3e38fbe124d77c250785da77c34cce039852c802c4e"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072417-slskdn.289/slskdn-main-linux-glibc-x64.zip"
    sha256 "04895cfbe24b2c196f2de9ede83106e11f25bc0054d85d3dd848f19b9626f59a"
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
