class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026072013-slskdn.285"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026072013-slskdn.285/slskdn-main-osx-arm64.zip"
      sha256 "1b30267dae90e9933ae55bcd67ebf99b6900c92d3e29bb0c03e07b79973146db"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026072013-slskdn.285/slskdn-main-osx-x64.zip"
      sha256 "5e009d555144623812e5a2ab484c662d7261b0d7875562e8fb875ca5e2ad74ee"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026072013-slskdn.285/slskdn-main-linux-glibc-x64.zip"
    sha256 "98164b53fc853283165cfcf46e6e8d62706c5f3b55273d27008fc50df5e3823f"
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
