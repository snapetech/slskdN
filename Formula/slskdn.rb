class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051400-slskdn.251"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051400-slskdn.251/slskdn-main-osx-arm64.zip"
      sha256 "cd8e74b98ddcc8d5c66a0aeff7a43bc024111693354c09dd11ace33c5736b245"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051400-slskdn.251/slskdn-main-osx-x64.zip"
      sha256 "a9abb07be543dae2850fffb6af635aa0dedbf1fa77b561503941ceb5821cf7b3"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051400-slskdn.251/slskdn-main-linux-glibc-x64.zip"
    sha256 "78345fc23e0d4d0a0f1c9efe53e20e4ce83f1b1fdcfd12d6c66fbb49b2918129"
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
