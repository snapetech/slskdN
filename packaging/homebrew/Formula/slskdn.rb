class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026052401-slskdn.268"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026052401-slskdn.268/slskdn-main-osx-arm64.zip"
      sha256 "176527c0664454b30e6d6cca53e8c45fe6f33a90542e4d4cddff0dac58b7367f"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026052401-slskdn.268/slskdn-main-osx-x64.zip"
      sha256 "c722db743b919e3dca4e768ad244fb72a9222edf34f2f1f16c5e7f7556b12f13"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026052401-slskdn.268/slskdn-main-linux-glibc-x64.zip"
    sha256 "5d2f7de82101cd87db8024ab677203be995e36fb9e414a61a90903f2aca67ced"
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
