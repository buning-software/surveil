<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![GNU Affero General Public License v3.0 License][license-shield]][license-url]

<div align="center">
  <h3 align="center">Surveil</h3>
  <p align="center">
    A lightweight Windows desktop application for real-time doorbell notifications and live camera feeds from your camera system. UniFi Protect is supported today, with more providers planned.
    <br />
    <a href="https://github.com/BuningSoftware/surveil/issues">Report Bug</a>
    ·
    <a href="https://github.com/BuningSoftware/surveil/issues">Request Feature</a>
  </p>
</div>

<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#features">Features</a></li>
        <li><a href="#supported-providers">Supported Providers</a></li>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
  </ol>
</details>

## About The Project

It's easy to miss doorbell activity while you're working on a PC. Surveil is a dedicated Windows client that sits on your desktop and makes sure you never miss a visitor. It connects your camera system to your workstation.

Surveil connects to camera systems through **providers**. Each provider implements the same set of capabilities: camera discovery, live video and event notifications. That lets the app work with different camera and NVR ecosystems. UniFi Protect is the first provider, and support for more is planned.

### Features

* **Real-time Doorbell Alerts:** Receive instant native Windows notifications when someone rings your doorbell.
* **Live Camera Feed:** Quickly open and view high-quality live streams from your cameras directly on your desktop.
* **Lightweight Performance:** Designed to run in the background with minimal CPU and RAM impact.
* **Provider-based:** Choose your camera provider in Settings, and changes apply without restarting the app.

### Supported Providers

| Provider | Status |
| --- | --- |
| [UniFi Protect](https://ui.com/camera-security) (UDM Pro, UNVR, Cloud Key, etc.) | ✅ Supported |
| Other cameras / NVRs | 🚧 Planned. [Request a provider](https://github.com/BuningSoftware/surveil/issues) |

### Built With

* [.NET](https://dotnet.microsoft.com/en-us/)
* [WinUI 3 / Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
* [LibVLCSharp](https://github.com/videolan/libvlcsharp)


## Getting Started

Setting up Surveil on your machine is straightforward.

### Installation

1. Download the latest `Surveil_<version>_<platform>.msix` for your architecture (x86, x64, or
   ARM64) and the accompanying `Surveil.cer` from the
   [Releases](https://github.com/BuningSoftware/surveil/releases) page.
2. Trust the publisher certificate (one-time step, since Surveil is self-signed rather than
   issued by a public CA): open an elevated PowerShell/Command Prompt and run
   ```
   certutil -addstore "TrustedPeople" Surveil.cer
   ```
   Alternatively, right-click `Surveil.cer` → **Install Certificate** → **Local Machine** →
   **Place all certificates in the following store** → **Trusted People**.
3. Double-click the `.msix` file and select **Install**. Windows should now recognize the
   publisher and install without a certificate warning.

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b features/feature-title`)
3. Commit your Changes (`git commit -m 'Add some feature'`)
4. Push to the Branch (`git push origin features/feature-title`)
5. Open a Pull Request

## License
Distributed under the GNU Affero General Public License v3.0 License. See `LICENSE` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>

[contributors-shield]: https://img.shields.io/github/contributors/BuningSoftware/surveil.svg?style=for-the-badge
[contributors-url]: https://github.com/BuningSoftware/surveil/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/BuningSoftware/surveil.svg?style=for-the-badge
[forks-url]: https://github.com/BuningSoftware/surveil/network/members
[stars-shield]: https://img.shields.io/github/stars/BuningSoftware/surveil.svg?style=for-the-badge
[stars-url]: https://github.com/BuningSoftware/surveil/stargazers
[issues-shield]: https://img.shields.io/github/issues/BuningSoftware/surveil.svg?style=for-the-badge
[issues-url]: https://github.com/BuningSoftware/surveil/issues
[license-shield]: https://img.shields.io/github/license/BuningSoftware/surveil.svg?style=for-the-badge
[license-url]: https://github.com/BuningSoftware/surveil/blob/main/LICENSE
