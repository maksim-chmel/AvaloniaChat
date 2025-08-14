# Secure P2P Chat (Avalonia + .NET)

## Overview
This application is a **cross-platform, secure, and anonymous peer-to-peer chat** built with **.NET**, **Avalonia UI**, and **Microsoft.Extensions.DependencyInjection** for dependency injection.

It allows users to **host or connect** to a chat session over the local network, with **end-to-end encryption** based on **RSA** for secure key exchange and **AES** for message encryption.

The app is designed to be lightweight, user-friendly, and privacy-focused.

---

## Features
- **Cross-platform**: Works on **Windows**, **Linux**, and **macOS**.
- **Secure Communication**:
  - **RSA** for exchanging AES session keys.
  - **AES** for encrypting chat messages.
- **P2P Chat**: Direct communication between clients without central servers.
- **Host & Connect Modes**: Start your own chat server or join an existing one.
- **Dependency Injection**: Implemented via `Microsoft.Extensions.DependencyInjection` for better maintainability.
- **Modern UI**: Built with Avalonia for a clean, responsive interface.

---

## Technologies Used
- **.NET 8**
- **Avalonia UI**
- **Microsoft.Extensions.DependencyInjection**
- **RSA / AES encryption**
- **TCP sockets** for networking

---

## Build & Run

### Requirements
- **.NET SDK 8.0+**
- (Optional) Avalonia Designer for XAML preview.

### Build for your platform

#### Windows
```sh
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
