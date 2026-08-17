class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026081618-slskdn.306"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026081618-slskdn.306/slskdn-main-osx-arm64.zip"
      sha256 "8daae6373bf46d2361e418401294c80547a76a17a5f42fc6e02e048e097e85ee"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026081618-slskdn.306/slskdn-main-osx-x64.zip"
      sha256 "34b2af1ce0f57e2da8845ae52e5163b035ae8d3ab0d893bcc5e542fcd1742389"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026081618-slskdn.306/slskdn-main-linux-glibc-x64.zip"
    sha256 "b386d7e51285916fcebff31c09e778d0a1c86cc98d4ed82c500ac57f7b22a9c8"
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
