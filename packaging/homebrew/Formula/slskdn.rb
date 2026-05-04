class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050400-slskdn.220"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050400-slskdn.220/slskdn-main-osx-arm64.zip"
      sha256 "09bcf0f340d10e9ccdde0c7f554b2afaa94695d1d91e1f2bbc4dd28c92c55dfa"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050400-slskdn.220/slskdn-main-osx-x64.zip"
      sha256 "5788a13428f07d3e81facf371544ca628eac96fbfbc75f69abc182b2474e0d81"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050400-slskdn.220/slskdn-main-linux-glibc-x64.zip"
    sha256 "58b85e79384e3fbe07ab14c34fd23baa31778ecbef16c147a2d6b1a749ad676b"
  end

  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
  end

  test do
    assert_match "slskd", shell_output("#{bin}/slskd --help", 1)
  end
end
