<div align=center>
<img src="https://github.com/TwilightLemon/Lemon-App/raw/refs/heads/master/LemonApp/Resources/icon.ico" width="128" height="128"/>

# Lemon App

✨ *Enjoy what you like* ✨

[<img src="https://img.shields.io/badge/license-GPL%203.0-yellow"/>](LICENSE.txt)
![C#](https://img.shields.io/badge/lang-C%23-orange)
![WPF](https://img.shields.io/badge/UI-WPF-b33bb3)
[![Release](https://img.shields.io/badge/Release-Lemon%20App-%23FF4D5B.svg?style=flat-squar)](https://github.com/TwilightLemon/Lemon-App/releases)
![GitHub Repo stars](https://img.shields.io/github/stars/TwilightLemon/Lemon-App)

</div>

## 🐱 Introduction

> A delicate and modern music player for Windows that syncs your playlists from NetEase Cloud Music and QQ Music — with stunning UI and animated lyrics.

- 🎵 **Playlist Sync**: Login to your **QQ Music** and **NetEase Cloud Music** account and sync your personal playlists (only content accessible by your own account).
- 🌈 **Modern UI Design**: Built with fluent-style acrylic and Mica effects (only available on Windows 11). Fully supports light and dark mode.
- 🪄 **Animated Lyrics**: Enjoy rich lyric effects with word-by-word highlights and smooth progress animations — inspired by Apple Music and Lyricify.
- 🚧 **Actively Developed**: Currently in **pre-release**. Some pages are still under construction. Contributions are welcome!

## 📦 Installation
Download the lastest release here: [Releases](https://github.com/TwilightLemon/Lemon-App/releases). Or build from source manually.

### Requirements

- ✅ [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ WebView2 Runtime (for Windows 10 users, required for login)
  - [Download WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- ✅ Visual Studio 2022+ with WPF development tools
- ✅ Windows 10+ (Windows 11 recommended)

### Build Steps

```bash
git clone https://github.com/TwilightLemon/Lemon-App.git
cd Lemon-App
dotnet build
```

## 🎨 Usage

### Login to QQ Music
> Due to the resistance of source API, you need to login to your QQ Music account to access most of the features. So you need to login first, then bind your NetEase Cloud Music account to sync playlists if needed.

### Start to Use
![HomePage](https://github.com/TwilightLemon/Data/blob/master/LA_HomePage.jpg?raw=true)
![Playlist](https://github.com/TwilightLemon/Data/blob/master/LA_PlaylistsPage.jpg?raw=true)
![LyricPage-Dark](https://github.com/TwilightLemon/Data/blob/master/LA_LyricPage-Dark.jpg?raw=true)
![LyricPage-Light](https://github.com/TwilightLemon/Data/blob/master/LA_LyricPage-Light.jpg?raw=true)

### Storage Location
The application data is stored in the following directory:
- **User Data**: `%APPDATA%\LemonAppNew` logs, settings, and cached data.
- **Resource Cache**: `VOL:\LemonApp` where `VOL` is usually the second drive of your system. This folder contains cached resources like album covers, lyrics and media files etc.
- **Download Location**: `%USERPROFILE%\Music\Lemon App` is the default download location.

### More to Explore
- **Quick Access**: Fix playlists, artists, albums, and rankings to the title bar for quick access.
- **Desktop Lyrics**: Enable desktop lyrics to display lyrics on your desktop. Plus, embed lyrics in your desktop wallpaper(Experimental).
- **Download Music**: Download music with high quality.
- **Custom Themes**: Highly customizable themes. You can use system accent color or calculated color from album cover. Acrylic and Mica effects are supported.

## 📄 LICENSE
This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE.txt) file for details.
### Third-Party Libraries

This project uses the following third-party libraries. We acknowledge and thank all contributors of these amazing tools.

| Library | Description | License |
|--------|-------------|---------|
| [EleCho.WpfSuite](https://github.com/OrgEleCho/EleCho.WpfSuite) | WPF layout panels, controls, value converters, markup extensions, transitions and utilities | MIT |
| [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) | JSON parsing library | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM framework helpers | MIT |
| [Lyricify Lyrics Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper) | Syllable Lyric Parser | Apache-2.0 license |
| [BASS](https://www.un4seen.com/) and [Bass.NET](https://www.un4seen.com/bass.html) | Audio library for music playback | non-commercial use |
| ... | ... | ... |

For each library, please refer to its own repository and license file for detailed terms.

### Disclaimer
See full [Disclaimer](DISCLAIMER.md)

## 🌐 Contributing
We welcome contributions to Lemon App! If you have ideas, bug reports, or feature requests, please open an issue or submit a pull request.
