class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026092418-slskdn.322"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026092418-slskdn.322/slskdn-main-osx-arm64.zip"
      sha256 "517ff90d46a0f0b739eea70029394f29c5371faf3583fee6e3fc1f948b4e6e69"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026092418-slskdn.322/slskdn-main-osx-x64.zip"
      sha256 "228d4d83ef46e77ee75b8b6bee962f326ceec18076e70d3e7dc73a7f11f27bab"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026092418-slskdn.322/slskdn-main-linux-glibc-x64.zip"
    sha256 "bdd2c22dd69ac0793933b88532832ed8101951e3461a3adcf49be8e75c9e4d98"
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
