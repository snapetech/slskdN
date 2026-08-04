class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080415-slskdn.299"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080415-slskdn.299/slskdn-main-osx-arm64.zip"
      sha256 "caedb96b49e5ae6ff53370c5b059cdfdaf8999606d58bc1649fb9133aac676f5"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080415-slskdn.299/slskdn-main-osx-x64.zip"
      sha256 "6f2d3c8e4bb5f6d2f47e973d536b167debab0dfb08bc54326cf3eb2a23efa56e"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080415-slskdn.299/slskdn-main-linux-glibc-x64.zip"
    sha256 "fc5c55021e5d5f57dda256dfe982298a428a771ca4bc1ae20710f64f8b9f3e64"
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
