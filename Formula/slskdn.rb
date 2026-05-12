class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051220-slskdn.244"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051220-slskdn.244/slskdn-main-osx-arm64.zip"
      sha256 "89237c114bb1e7109e376f7fa64f0b1ebc4fcc4f833a82b01195e49a6d5cc55f"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051220-slskdn.244/slskdn-main-osx-x64.zip"
      sha256 "347630d934ab3905ae03aac32b9dc4f747d730f2a98897ebf279595b288a5440"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051220-slskdn.244/slskdn-main-linux-glibc-x64.zip"
    sha256 "40c57a5740082dfef1e3ba942522b9fce363b6ba0e178bad1f8a1e300df7bf36"
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
