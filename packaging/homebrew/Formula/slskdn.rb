class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050300-slskdn.219"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050300-slskdn.219/slskdn-main-osx-arm64.zip"
      sha256 "29af2cd61ece2e0db2febc166538d1590b22ad3c9d08c83cc2b9cb1e15db6d71"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050300-slskdn.219/slskdn-main-osx-x64.zip"
      sha256 "9fe5aa1071c6e1af22d558f4dfc286102c222e1e81ae6c96972dc1a164bd5ec5"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050300-slskdn.219/slskdn-main-linux-glibc-x64.zip"
    sha256 "0080b499309c8905db403979fdda67f2668dc29eec5deaf3b462f3533fdf8f08"
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
