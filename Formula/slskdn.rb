class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051600-slskdn.259"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.259/slskdn-main-osx-arm64.zip"
      sha256 "89797ed09089b813e981fa0f8819fac1552bdb159210866b8e2ddf4b726eddb1"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.259/slskdn-main-osx-x64.zip"
      sha256 "ff12f6feb34814514ba85d14212db53cf7a081bbf4fd8894935c32c0e7d61099"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.259/slskdn-main-linux-glibc-x64.zip"
    sha256 "1c658abfef1027eeb742ae0ad5c3f8eabacf226e440891a628f352a380bb61de"
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
