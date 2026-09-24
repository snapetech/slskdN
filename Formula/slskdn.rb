class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092420-slskdn.323"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092420-slskdn.323/slskdn-main-osx-arm64.zip"
      sha256 "ffb8983c760afbefa78c47118c44b1d19f8b74c11e2819594894ed6a212a6c6e"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092420-slskdn.323/slskdn-main-osx-x64.zip"
      sha256 "e17b45d714d0968fccb52dda2f6d1b7e7a8a31211c372a6c3c4da46f21ab9191"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092420-slskdn.323/slskdn-main-linux-glibc-x64.zip"
    sha256 "b211631c243656753356374846dd7c9519bcd8291a83693d612948982e50fe77"
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
