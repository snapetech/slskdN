class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026051217-slskdn.240"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026051217-slskdn.240/slskdn-main-osx-arm64.zip"
      sha256 "a4caab4f08088b699a07e62e1e3d342d0ee31a46787105e5cc73ed31f718df5b"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026051217-slskdn.240/slskdn-main-osx-x64.zip"
      sha256 "270178caab5131b94861db0be5390037651ef647ea5d31028f3e751757237bc0"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026051217-slskdn.240/slskdn-main-linux-glibc-x64.zip"
    sha256 "56731c9f90653ce1f391cc334b96b499f3e98b101aee2e73648c434e555c5675"
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
