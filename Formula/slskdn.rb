class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100421-slskdn.338"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100421-slskdn.338/slskdn-main-osx-arm64.zip"
      sha256 "8b57ae11b819fe6e236f4589b945d7062d3051f23a1e2ecc65a76f1146375003"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100421-slskdn.338/slskdn-main-osx-x64.zip"
      sha256 "da12e11188cd7b1a636c8cbf5f7d142adbb9fdeeee1cfc21b7e3b09d2026993a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100421-slskdn.338/slskdn-main-linux-glibc-x64.zip"
    sha256 "3d5a1bedf7ffd078279dcde36da42977fd48853c7e73f844f4af55f400f0bb1f"
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
