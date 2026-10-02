<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![GNU Affero General Public License v3.0 License][license-shield]][license-url]

<div align="center">
  <h3 align="center">Surveil</h3>
  <p align="center">
    A lightweight Windows desktop application to receive real-time doorbell notifications and view camera feeds from your Unifi Protect deployment.
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

Many Unifi Protect users find it difficult to keep track of doorbell activity while working on a PC. This project provides a dedicated Windows client that sits in your tray or on your desktop, ensuring you never miss a visitor. It bridges the gap between your Ubiquiti ecosystem and your primary workstation.

### Features

* **Real-time Doorbell Alerts:** Receive instant Windows native notifications when someone rings your Unifi Doorbell.
* **Live Camera Feed:** Quickly open and view high-quality streams from your Protect cameras directly on your desktop.
* **Lightweight Performance:** Designed to run in the background with minimal CPU and RAM impact.
* **Easy Integration:** Connects securely to your Unifi Console (UDM Pro, UNVR, etc.) using standard credentials.

### Built With

* [.NET](https://dotnet.microsoft.com/en-us/)


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
