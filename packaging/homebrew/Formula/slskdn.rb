class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051603-slskdn.260"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051603-slskdn.260/slskdn-main-osx-arm64.zip"
      sha256 "611743bd8fb5103c0b7249de4c58b27ddcdebc87a215399cd38e0695e4c2e26d"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051603-slskdn.260/slskdn-main-osx-x64.zip"
      sha256 "0516df2492fee7ce813dc5e38a482dcd04cc31a03c14f0c40ff7520f53c8503a"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051603-slskdn.260/slskdn-main-linux-glibc-x64.zip"
    sha256 "9df80abae18598e5d746a3742b8ed13e7b4662e3763b66c6aa25908bf3af5798"
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
