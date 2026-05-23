class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052301-slskdn.267"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052301-slskdn.267/slskdn-main-osx-arm64.zip"
      sha256 "bcfb51c3b9136e7fcb015871a60ffbd08c33c8a3958ad1c02c44ffc3d9aa1d82"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052301-slskdn.267/slskdn-main-osx-x64.zip"
      sha256 "43e6cb797ba483f23e9a4cdf52169faff0ac9606970364cf36ab1be3223f0029"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052301-slskdn.267/slskdn-main-linux-glibc-x64.zip"
    sha256 "c0dee21c06f067a6726f16e29f34f3ceee9bcdb25aab8c85df8881c2f13fd18f"
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
