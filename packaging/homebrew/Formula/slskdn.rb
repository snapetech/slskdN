class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050500-slskdn.223"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.223/slskdn-main-osx-arm64.zip"
      sha256 "0cbe83723d5964034cd8998ebe1e7c5e7ff9c4478cdaa39ee91fd02070b98af9"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.223/slskdn-main-osx-x64.zip"
      sha256 "a4d5307adacd72492cdc50a7c05981e5f4d16b15104872e13fa90e16459640cf"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050500-slskdn.223/slskdn-main-linux-glibc-x64.zip"
    sha256 "f995ccc68b637255f4ac93c0460bb652ba34d736c22f5d2d6932943afc885331"
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
