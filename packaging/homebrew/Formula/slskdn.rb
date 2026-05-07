class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050720-slskdn.233"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050720-slskdn.233/slskdn-main-osx-arm64.zip"
      sha256 "d3dfc63df70281dbe78f704dbca00de610577422af38d58d93316f6e6555b5eb"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050720-slskdn.233/slskdn-main-osx-x64.zip"
      sha256 "7ab4401d4055136c8e6705abfb109804d27f635e38f2010b90e98d69e51a1b86"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050720-slskdn.233/slskdn-main-linux-glibc-x64.zip"
    sha256 "5b0449871aff9bda782fe5b57836d26af2377c54f1db510b8e15359f5ccaa91b"
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
