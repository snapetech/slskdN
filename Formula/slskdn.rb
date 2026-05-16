class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051600-slskdn.258"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.258/slskdn-main-osx-arm64.zip"
      sha256 "d01eb3e730a49dc9715d470ae267d4de6bf5b7695d09d9fd5268ec0fc022dd8b"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.258/slskdn-main-osx-x64.zip"
      sha256 "e7646460f39705ca8c12ecc854f68a5f89d10db98bb7b4700997701eaeca3d3a"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051600-slskdn.258/slskdn-main-linux-glibc-x64.zip"
    sha256 "2c7d2688f6da7939a98d9936cd3d2bbe012199a311e2646bbc83a3b454597da8"
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
