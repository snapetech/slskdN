class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071717-slskdn.279"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071717-slskdn.279/slskdn-main-osx-arm64.zip"
      sha256 "d156ba7d371608434c27f76920e86f430467dfe43c697ca60923d418844b9872"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071717-slskdn.279/slskdn-main-osx-x64.zip"
      sha256 "26c48abcdacdef33d9af91b6cc9f2be7697e8365cc0c04344e350282137ae1c6"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071717-slskdn.279/slskdn-main-linux-glibc-x64.zip"
    sha256 "46a6a13ab6644f0ecf439aec32601aae250774da28f6eba77db5249d17d90b72"
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
