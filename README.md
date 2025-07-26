<div align="center">
  <p>
    <a href="README-tr.md">Türkçe Versiyon</a>
  </p>
</div>

<div align="center">

  <!-- Project Logo -->
  <!-- <img src="#" alt="SecureVault Logo" width="150"/> -->
  
  <h1 style="border-bottom: none;">SecureVault</h1>
  
  **A modern, cross-platform password manager with a focus on security and privacy, developed with .NET 9, MAUI and microservices architecture.**

</div>

<p align="center">
  <a href="https://github.com/kursatabayli/securevault/blob/develop/LICENSE"><img src="https://img.shields.io/github/license/kursatabayli/securevault?style=for-the-badge&color=blue" alt="License"></a>
  <img src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/badge/MAUI-Multi--Platform-5C2D91?style=for-the-badge&logo=c-sharp" alt="MAUI">
  <img src="https://img.shields.io/badge/Architecture-Microservices-orange?style=for-the-badge" alt="Microservices Architecture">
</p>

This project is an implementation of a modern password and 2FA code manager with a strong focus on security and privacy. It is designed based on a **Zero-Knowledge** architecture, meaning the server has no knowledge of the user's master password or the data stored in the vault. All encryption and decryption operations are performed client-side.

---

### 🏛️ Core Architectural Features

The architecture is built upon several key technical principles to ensure security and scalability.

| Feature | Implementation Details |
| :--- | :--- |
| 🛡️ **Zero-Knowledge Authentication** | Authentication is handled via a challenge-response mechanism using **ECDSA signatures**. The user's master password never leaves the client device. |
| ✍️ **End-to-End Encryption (E2EE)** | All vault data is encrypted client-side using **AES-256 (GCM)** before being transmitted to the server, ensuring data remains confidential even if the server is compromised. |
| 🌐 **Cross-Platform Client** | A single codebase using **.NET MAUI Blazor** targets both mobile (Android) and desktop (Windows) platforms, providing a consistent UI and native performance. |
| 🧩 **Microservice-Based Backend** | The backend is composed of decoupled services (Identity, Vault) orchestrated via an **Ocelot API Gateway** and **Consul** for service discovery, promoting resilience and scalability. |

---

### ⚙️ Technology Stack

This project utilizes a modern, robust technology stack to achieve its goals.

| Category | Technology / Library | Purpose |
| :--- | :--- | :--- |
| **Backend** | **.NET 9, ASP.NET Core** | Core framework for building high-performance, modern APIs. |
| | **MediatR** | Implementation of the CQRS pattern to decouple business logic. |
| | **Ocelot** | API Gateway for request routing, rate limiting, and aggregation. |
| **Frontend**| **.NET MAUI Blazor** | Cross-platform client application framework. |
| | **MudBlazor** | A rich component library for building a clean and responsive UI. |
| **Data & Cache**| **PostgreSQL** | Primary relational database with JSONB support. |
| | **Entity Framework Core** | ORM for secure and object-oriented database interaction. |
| | **Redis** | In-memory cache for challenges and temporary session data. |
| **DevOps** | **Docker, Docker Compose** | Containerization for consistent development and deployment environments. |
| | **Consul** | Service Discovery for dynamic microservice registration and lookup. |
| **Security** | **BouncyCastle** | Advanced cryptography, specifically for ECDSA signature generation. |
| | **Argon2id** | State-of-the-art password hashing algorithm for Master Key derivation. |
| | **AES-256 (GCM)** | Authenticated symmetric encryption for E2EE. |
| | **JWT (JSON Web Token)** | Secure and stateless session management via Access & Refresh Tokens. |

---

### 🔐 Security Architecture Deep Dive

The security model is designed to ensure that only the user can access their data.

#### 1. Registration and Key Generation Flow
1.  A unique `salt` is generated on the client.
2.  The user's master password and the `salt` are fed into **Argon2id** to derive a 32-byte `Master Key`.
3.  The `Master Key` is then used with **HKDF** to derive two separate keys:
    * `Authentication Key (Private Key)`: For signing authentication challenges.
    * `Encryption Key`: For encrypting and decrypting vault data.
4.  The `Authentication Key` is used to generate a corresponding `Public Key` via **ECDSA (secp256k1)**.
5.  Only the `Public Key` and the `salt` are sent to the server. The master password and derived keys never leave the client.

#### 2. Authentication Flow
1.  The client requests a random `challenge` string from the server.
2.  On the client, the `Master Key` and `Authentication Key` are re-derived using the stored password and fetched `salt`.
3.  The `challenge` is signed with the `Authentication Key`.
4.  The resulting signature is sent to the server, which verifies it against the user's stored `Public Key`. A valid signature grants access.

---

### 🚀 Project Roadmap

| Feature | Description | Status |
| :--- | :--- | :--- |
| Zero-Knowledge Authentication | Passwordless, proof-based login system. | ✅ **Completed** |
| End-to-End Encryption | On-device encryption of vault data. | ✅ **Completed** |
| 2FA Vault (TOTP/HOTP) | Add/list 2FA codes via QR code and manual entry. | ✅ **Completed** |
| Advanced Session Management | View and remotely terminate active sessions. | 🚧 **In Progress** |
| Vault Data Editing | Update existing password and 2FA records. | 🚧 **In Progress** |
| Login with QR Code | Fast and secure login on web or other devices. | 📋 **Planned** |
| Recovery Key | A secure mechanism for data recovery if the password is forgotten. | 📋 **Planned** |
| Autofill | Autofill passwords and codes in mobile apps and browsers. | 💡 **Considering** |

---

### 🛠️ Local Setup and Launch

Follow these steps to run the project on your local machine.

#### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker and Docker Compose](https://www.docker.com/products/docker-desktop/)
* [Git](https://git-scm.com/)

#### Installation Steps
1.  **Clone the Project:**
    ```bash
    git clone https://github.com/kursatabayli/SecureVault.git
    cd securevault
    ```

2.  **Set Up Environment Variables:**
    Create a file named `.env` in the root directory of the project. This file will contain sensitive information used by `docker-compose.yml`.

    **Example `.env` file content:**
    ```env
    # PostgreSQL Settings
    DB_USER=securevaultuser
    DB_PASSWORD=YourStrongPasswordHere!123
    DB_NAME=securevaultdb

    # JWT Settings (Should be at least 32 random characters)
    JWT_KEY=YourSuperSecretKeyForJwtTokens32CharactersLong
    JWT_REFRESH_KEY=AnotherSuperSecretKeyForRefreshTokens32CharsLong

    # Consul Settings
    CONSUL_HOST=consul

    # Docker Network Settings
    FORWARDED_HEADERS_KNOWN_NETWORKS=172.22.0.0/16
    ```

3.  **Launch with Docker:**
    Run the following command in the root directory.
    ```bash
    docker-compose up -d --build
    ```

4.  **Check Service Status:**
    Ensure all services are in the `Up` or `healthy` state.
    ```bash
    docker-compose ps
    ```

5.  **Run the Application:**
    * **API Gateway:** Accessible at `https://localhost:7202`.
    * **MAUI Client:** Open the `src/clients/SecureVault.App` project in Visual Studio and run it for your desired platform (Windows or Android).

---
