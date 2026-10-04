class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100420-slskdn.337"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100420-slskdn.337/slskdn-main-osx-arm64.zip"
      sha256 "265cae951817a166d97b7f783d50ec2f64e42de70ad6fe6e442d1b3d516b1322"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100420-slskdn.337/slskdn-main-osx-x64.zip"
      sha256 "6a69f8a3bdcb24be75a7b303e80a7b8458648ef65663014adc77df176b372543"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100420-slskdn.337/slskdn-main-linux-glibc-x64.zip"
    sha256 "9692a57d8540e6059ff3f1e5f96e3527edcef235097544cd5f2c21ad73c5772f"
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
