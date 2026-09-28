class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092820-slskdn.328"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092820-slskdn.328/slskdn-main-osx-arm64.zip"
      sha256 "fe285d1acdea66fb5599cf0e5652a344c16ade0950af598ee7fa7073aff50e21"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092820-slskdn.328/slskdn-main-osx-x64.zip"
      sha256 "b9324194e34977336664184857105ed1c945089609db265727cf3738b3ad19b7"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092820-slskdn.328/slskdn-main-linux-glibc-x64.zip"
    sha256 "40f79483e5cd22360e6d3224c513184db235b5f38bba0bc0eeb74dd39e30d4a9"
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
