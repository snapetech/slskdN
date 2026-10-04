class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100417-slskdn.336"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100417-slskdn.336/slskdn-main-osx-arm64.zip"
      sha256 "a69091fab81a679f1c02d3b3fbd7ef6312569ac7266e7902e4bfe56fad50f685"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100417-slskdn.336/slskdn-main-osx-x64.zip"
      sha256 "2dd7d99a797a0d329e04c6d3c8268d977604eb5a77b693b8b0bf8530262984de"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100417-slskdn.336/slskdn-main-linux-glibc-x64.zip"
    sha256 "fc6ac50929bbd1ddd769e0183da5d49ae466dff64916344e6ddb51c55ad30b28"
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
