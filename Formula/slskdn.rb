class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026100423-slskdn.339"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026100423-slskdn.339/slskdn-main-osx-arm64.zip"
      sha256 "83b3ecd8d8d543d6387cfe9c644cdd4e9fe29140c0b334f3871d423e97ee21f2"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026100423-slskdn.339/slskdn-main-osx-x64.zip"
      sha256 "bc38421c577903accb6e2b72bb1d09431f741c46e9d75182df628568339fd40f"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026100423-slskdn.339/slskdn-main-linux-glibc-x64.zip"
    sha256 "f59df643cdc56895ab84ab70e3a9977bd50799b85487d0b8ed05d2bdfbf7a8b0"
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
