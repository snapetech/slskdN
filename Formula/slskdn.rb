class Slskdn < Formula
  desc "Unofficial slskd fork with batteries-included Soulseek features"
  homepage "https://github.com/snapetech/slskdn"
  license "AGPL-3.0-or-later"
  version "2026050712-slskdn.232"
  on_macos do
    on_arm do
      url "https://github.com/snapetech/slskdn/releases/download/2026050712-slskdn.232/slskdn-main-osx-arm64.zip"
      sha256 "6d68ba2a299d19fac8bc48eb3f9a2cbdc3180eafafade36decfc1ccf5bee7321"
    end
    on_intel do
      url "https://github.com/snapetech/slskdn/releases/download/2026050712-slskdn.232/slskdn-main-osx-x64.zip"
      sha256 "9e70a4fcad7d80c1d561e5422f53b5de6c11c06a0b8312d89d107c82ff01dedb"
    end
  end
  on_linux do
    url "https://github.com/snapetech/slskdn/releases/download/2026050712-slskdn.232/slskdn-main-linux-glibc-x64.zip"
    sha256 "019ea295fc89cdc7db1c86de9faf1baa01d1ba502ed73f12fe73360b823ff671"
  end
  def install
    libexec.install Dir["*"]
    (bin/"slskd").write_exec_script libexec/"slskd"
    (bin/"slskdn").write_exec_script libexec/"slskd"
  end
end
