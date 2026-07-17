class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026071721-slskdn.283"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026071721-slskdn.283/slskdn-main-osx-arm64.zip"
      sha256 "f831feb33fc6289ec288b7a6cb2030de0c32532df8906046923b2b7c583335a0"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026071721-slskdn.283/slskdn-main-osx-x64.zip"
      sha256 "d417ddb7060d2c6143e581ca177d0263169076fef360201ee87d20be90329c56"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026071721-slskdn.283/slskdn-main-linux-glibc-x64.zip"
    sha256 "83a31ebfc4c2aa883f48daf0825043006bfde17619405b5a704421aa23ea9bd2"
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
