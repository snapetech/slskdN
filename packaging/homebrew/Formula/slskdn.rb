class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050600-slskdn.227"

  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050600-slskdn.227/slskdn-main-osx-arm64.zip"
      sha256 "cebbba56d81772d4fc51e09381905bd317aabfa587c1f9a34dc1ace52e963c04"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050600-slskdn.227/slskdn-main-osx-x64.zip"
      sha256 "9e68be6d3e5db747098e3aa70a6d9ccd74bcbb14908f1e58d69839e2838d0884"
    end
  end

  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050600-slskdn.227/slskdn-main-linux-glibc-x64.zip"
    sha256 "65af1323c194db0f9bdd0a46d86e676f278a3525d78b8fc8974bab2a295ba6c4"
  end

  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
  end

  test do
    assert_match "slskd", shell_output("#{bin}/slskd --help", 1)
  end
end
