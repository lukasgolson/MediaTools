If they do not already exist, you must put the FFmpeg shared build binaries into this folder: avcodec, avformat, avutil, swresample, swscale.

Windows - You can download it from the https://github.com/BtbN/FFmpeg-Builds/releases. You only need *.dll files from the .\bin directory (not .\lib) of the ZIP package. Place the binaries in the .\ffmpeg\x86_64\(64bit) in the application output directory or set FFmpegLoader.FFmpegPath.
Linux - Download FFmpeg using your package manager.
macOS, iOS, Android - Not supported.