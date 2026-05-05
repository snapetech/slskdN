class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050500-slskdn.224"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.224/slskdn-main-osx-arm64.zip"
      sha256 "a012a08c3b585f7522cf666ed27af5c16021667b5c2c30a5baec81b2ca9a75ac"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.224/slskdn-main-osx-x64.zip"
      sha256 "3aeecb7e0044a27065133067701811bcf45b2f42346185c862c375aa63e4a675"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.224/slskdn-main-linux-glibc-x64.zip"
    sha256 "6960c31278404b66a7bdca6d0faa20e476864090375057d5e5a02a0d197a3c8b"
  end
  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
  end
end
