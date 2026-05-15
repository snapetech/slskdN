class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051501-slskdn.253"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051501-slskdn.253/slskdn-main-osx-arm64.zip"
      sha256 "125e3c37bdaa2537151033cd0f53638b012881dd09a640c9d6b9ffd8d17a0dde"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051501-slskdn.253/slskdn-main-osx-x64.zip"
      sha256 "a50dee37a2fcb3308e840a67dbee8114b75fdcfb33f84912deea83ab0ac76a4e"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051501-slskdn.253/slskdn-main-linux-glibc-x64.zip"
    sha256 "44164308ccedc1d18bb79680b28e4f18eb24f3cf146f6843bb6584d75227cf73"
  end

  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
    if (libexec/"vpn-agent/slskdN-vpn-agent").exist?
      (bin/"slskdN-vpn-agent").write_exec_script libexec/"vpn-agent/slskdN-vpn-agent"
    end
  end

  test do
    assert_match "slskd", shell_output("#{bin}/slskd --help", 1)
    if (bin/"slskdN-vpn-agent").exist?
      assert_match "slskdN-vpn-agent", shell_output("#{bin}/slskdN-vpn-agent --help")
    end
  end
end
