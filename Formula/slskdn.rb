class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026080316-slskdn.297"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026080316-slskdn.297/slskdn-main-osx-arm64.zip"
      sha256 "1415675493df152b93d866fe61c6a3bc1f26dfbb64a0d9670185d7963e7f5093"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026080316-slskdn.297/slskdn-main-osx-x64.zip"
      sha256 "a73198895baa5457d7915b0a72843ce6dd59b57fabe549abbb5e6d609516f83a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026080316-slskdn.297/slskdn-main-linux-glibc-x64.zip"
    sha256 "7642ed00524b201abfe5d230e79e52b61872ea19b8703b2a873a58a4008e9998"
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
