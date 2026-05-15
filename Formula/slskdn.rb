class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051509-slskdn.256"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.256/slskdn-main-osx-arm64.zip"
      sha256 "22bf0b18c7ce85d5e962f3309e3c5eab2c44ef46a455443192c045f4d19934f8"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.256/slskdn-main-osx-x64.zip"
      sha256 "0023656f6f2ce489eab8c3769af39b1590778f0b867ef40e7d23ddc8bacf3c5c"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051509-slskdn.256/slskdn-main-linux-glibc-x64.zip"
    sha256 "2d570e006b4426c8ea17caa7e73870a1be7f5d9e2fa425d32598203a4fe72c3b"
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
