<div align="center">
  <p>
    <a href="README-tr.md">Türkçe Versiyon</a>
  </p>
</div>

<div align="center">
  
  <h1 style="border-bottom: none;">Secure Vault</h1>
  
  **A modern, cross-platform password manager focused on security and privacy, built with .NET 9, MAUI, and a microservices architecture.**

</div>

<p align="center">
<a href="https://github.com/kursatabayli/SecureVault/blob/master/LICENSE"><img src="https://img.shields.io/github/license/kursatabayli/SecureVault?style=for-the-badge&color=blue" alt="License"></a>
  <img src="https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/badge/MAUI-Multi--Platform-5C2D91?style=for-the-badge&logo=c-sharp" alt="MAUI">
  <img src="https://img.shields.io/badge/Architecture-Microservices-orange?style=for-the-badge" alt="Microservices Architecture">
</p>

> **⚠️ Project Status: Under Development**
>
> This project is under active development, and its primary purpose is to showcase technical concepts. The software is provided "as is" without any warranty. It is **not yet considered stable for production use**. Please do not use it to store real and sensitive credentials at this stage.

This project is a modern password and 2FA code manager application focused on security and privacy. The project is designed based on a **Zero-Knowledge** architecture. This means the server has no knowledge of the user's master password or the data stored in the vault. All encryption and decryption operations are performed on the client side.

---

### ✨ Key Features

* **Zero-Knowledge Architecture:** Servers can never see your master password or vault data.
* **End-to-End Encryption (E2EE):** All data is encrypted with AES-26 (GCM) before leaving the device.
* **Strong Cryptography:** Uses Argon2id for password derivation, ECDSA for authentication, and ECDH for secure key exchange.
* **Cross-Platform Support:** A single codebase for Windows and Android with .NET MAUI Blazor.
* **Real-Time Synchronization:** Changes made on one device are instantly reflected on your other devices (via SignalR).
* **Secure QR Code Login:** Log in securely and encrypted by scanning a QR code with your mobile device, without entering your password.
* **Offline Access:** Your vault data is stored locally using SQLite, so you can access it even without an internet connection.
* **Open Source:** Fully transparent and community-auditable code.

---

### 🏛️ Architecture and Security Model

The architecture is built on modern design patterns and technologies to ensure security, scalability, and flexibility.

| Feature                          | Implementation Details                                                                                                                                                             |
| :------------------------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 🛡️ **Zero-Knowledge Authentication** | Authentication is performed with a challenge-response mechanism using **ECDSA signatures**. The user's master password never leaves the client device.                                   |
| ✍️ **End-to-End Encryption (E2EE)** | All vault data is encrypted on the client side with **AES-256 (GCM)** before being sent to the server. This ensures data remains confidential even if the server is compromised.     |
| 🔑 **Secure Login with QR Code** | A temporary and secure encryption channel is established between devices using **ECDH (Elliptic Curve Diffie-Hellman)** key exchange. Session information is transmitted over this channel with E2EE. |
| 🌐 **Cross-Platform Client** | A single codebase developed using **.NET MAUI Blazor** targets both mobile (Android) and desktop (Windows) platforms, offering a consistent UI and native performance.              |
| 🧩 **Microservices-Based Backend** | The backend consists of independent services (Identity, Vault, Interaction). Inter-service communication is managed by **Ocelot API Gateway**, service discovery by **Consul**, and asynchronous messaging by **RabbitMQ**. |
| 🔄 **Real-Time Data Streaming** | **SignalR** establishes a bidirectional, persistent connection between clients and the server, enabling instant delivery of data changes to all devices.                                 |
| 💾 **Hybrid Data Storage** | On the client side, data is stored in an **SQLite** database for offline access. On the server side, **PostgreSQL** and **MongoDB** are used based on service needs.                   |

---

### 🔐 A Deep Dive into the Architecture

## Project Structure
The project consists of independent components, each responsible for a specific function:

* **MAUI Blazor Client:** The cross-platform application that the user interacts with, running on Windows and Android. All cryptographic operations happen here.
* **Ocelot API Gateway:** A central gateway that receives requests from the outside world, performs authentication and authorization checks, and routes requests to the appropriate microservice.
* **Identity Service:** Responsible for user registration, authentication, session management, and general user data. It uses PostgreSQL as its database.
* **Vault Service:** Responsible for storing the user's encrypted vault data (passwords, 2FA codes, etc.). It uses MongoDB as its database.
* **Interaction Service:** Manages real-time communication between devices (with SignalR) and asynchronous events (via RabbitMQ). QR code login and instant data synchronization are handled by this service.

## Cryptographic Flows
The security model is designed to ensure that only the user can access their own data.

1. **Registration and Key Generation Flow**
    * A unique `salt` is generated on the client side.
    * The user's master password and `salt` are transformed into a 32-byte `Master Key` using Argon2id.
    * The `Master Key` is used with **HKDF** to derive two separate keys:
      * `Private Key`: For signing authentication challenges.
      * `Encryption Key`: For encrypting and decrypting vault data.
    * A `Public Key` is calculated from the `Private Key` using **ECDSA (secp256k1)**.
    * Only the `Public Key` and `salt` are sent to the server. The master password and other derived keys never leave the client device.

2. **Password-Based Authentication Flow**
   * The client requests a random `challenge` text from the server.
   * On the client side, the `Master Key` and `Private Key` are momentarily regenerated using the stored password and the `salt` received from the server.
   * The `challenge` is signed with the `Private Key`.
   * The generated signature is sent to the server. The server verifies this signature with the user's stored `Public Key`. A valid signature grants access.

3. **QR Code Login Flow (with ECDH)**
    * The new device wanting to log in (Requester) or an already logged-in authorized device (Provider) creates a channel via the Interaction service and displays the channel ID as a QR code.
    * The Provider or Requester scans this QR code.
    * Both devices generate a temporary key pair using **ECDH (Elliptic Curve Diffie-Hellman)** and send their public keys to each other via SignalR.
    * Each party calculates a common `shared secret` using their own private key and the other party's public key. This secret is never transmitted over the network.
    * The Provider device encrypts the user's `Encryption Key` and `Private Key` with this `shared secret` and sends it to the Requester device.
    * The Requester decrypts the encrypted package with its own `shared secret` to obtain the necessary keys for logging in and completes the standard authentication flow.

4. **Real-Time Synchronization Flow
    * When a user adds, updates, or deletes vault data on a device, the change is saved to the local SQLite database and also sent encrypted to the Vault service.
    * After saving the data, the Vault service publishes an event like "UserActivityOccurred" to **RabbitMQ**.
    * The **Interaction service** listens for this event from RabbitMQ.
    * When it catches the event, it sends a "SyncRequired" notification via **SignalR** to all other active devices of the relevant user.
    * Other clients that receive this notification pull the latest changes from the server to update their local databases.

---

### ⚙️ Technology Stack

This project uses a modern and robust technology stack to achieve its goals.

| Category               | Technology / Library      | Purpose                                                                   |
| :--------------------- | :------------------------ | :------------------------------------------------------------------------ |
| **Backend** | **.NET 9, ASP.NET Core** | The main framework for building high-performance, modern APIs.            |
|                        | **MediatR** | To implement the CQRS pattern and separate business logic into commands/queries. |
|                        | **Ocelot** | API Gateway; request routing, rate limiting, and centralized management.  |
| **Frontend** | **.NET MAUI Blazor** | Cross-platform (Windows, Android) client application framework.           |
|                        | **MudBlazor** | A rich component library for creating a clean and responsive UI.          |
| **Data & Cache** | **PostgreSQL** | The primary database for relational and JSONB data.                       |
|                        | **MongoDB** | NoSQL document database for the Vault service.                            |
|                        | **SQLite** | Client-side local database.                                               |
|                        | **Entity Framework Core** | ORM; type-safe, object-oriented interaction with the database.            |
|                        | **Redis** | High-performance cache service; for challenges and temporary session data. |
| **Messaging & Real-time** | **RabbitMQ** | Asynchronous, event-driven communication between microservices.           |
|                        | **SignalR** | Real-time data synchronization and messaging between devices.             |
| **API Communication** | **Refit** | Type-safe REST client for .NET, simplifies API calls.                     |
| **Logging & Monitoring** | **Serilog** | Flexible and configurable structured logging library.                     |
|                        | **Seq** | Centralized log collection and analysis server, integrated with Serilog.  |
| **DevOps** | **Docker, Docker Compose**| Containerization; consistent development and deployment environments.     |
|                        | **Consul** | Service Discovery; for microservices to find each other in a dynamic environment. |
| **Security** | **BouncyCastle** | Advanced cryptography operations (especially ECDSA signing).              |
|                        | **Argon2id** | Strong password hashing; brute-force resistant Master Key derivation.     |
|                        | **AES-256 (GCM)** | Modern, authenticated symmetric encryption for end-to-end data encryption.|
|                        | **JWT (JSON Web Token)** | Secure and stateless session management (Access & Refresh Tokens).        |
| **QR Code Operations** | **ZXing.Net.MAUI** | Client-side QR code reading and scanning.                                 |
|                        | **QRCoder & SkiaSharp** | QR code generation and rendering.                                         |

---

### 🚀 Project Roadmap

| Feature                     | Description                                                                       | Status                     |
| :-------------------------- | :-------------------------------------------------------------------------------- | :------------------------- |
| Zero-Knowledge Authentication | Proof-based login system without sending passwords to the server.                 | ✅ **Completed** |
| End-to-End Encryption       | Encryption of vault data on the device before it reaches the server.              | ✅ **Completed** |
| 2FA Vault (TOTP/HOTP)       | Add and list 2FA codes by scanning a QR code, uploading an image, or manual entry. | ✅ **Completed** |
| Secure Login with QR Code   | Secure login via an E2EE channel using ECDH.                                      | ✅ **Completed** |
| Real-Time Synchronization   | Instant data sync between devices (with SignalR).                                 | ✅ **Completed** |
| Offline Mode (Local DB)     | Client-side SQLite for offline data access.                                       | ✅ **Completed** |
| Advanced Session Management | List active sessions and terminate them remotely.                                 | ✅ **Completed** |
| Recovery Key                | A secure mechanism for data recovery in case of a forgotten password.             | 🚧 **In Progress** |
| Edit Vault Data             | Update existing password and 2FA entries.                                         | 🚧 **In Progress** |
| Autofill                    | Autofill passwords and codes on mobile and in browsers.                           | 💡 **Under Consideration** |

---

### 🛠️ Local Setup and Launch

Follow the steps below to run the project on your local machine.

#### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker and Docker Compose](https://www.docker.com/products/docker-desktop/)
* [Git](https://git-scm.com/)

#### Setup Steps
1.  **Clone the Project:**
    ```bash
    git clone [https://github.com/kursatabayli/SecureVault.git](https://github.com/kursatabayli/SecureVault.git)
    cd securevault
    ```

2.  **Set Up Environment Variables:**
    In the project's root directory, copy the `.env.example` file to create a new file named `.env`. This file contains sensitive information like database connection strings and JWT keys, which will be used by `docker-compose.yml`.
    Edit the `.env` file with your own settings.

    ```bash
    cp .env.example .env
    # Open and edit the .env file with a text editor
    ```
    **Example `.env` file content:**
    ```env
    # PostgreSQL Settings (for Identity service)
    DB_USER=postgres
    DB_PASSWORD=postgre_super_secret_password
    DB_NAME=securevault_identity_dev
    DB_HOST=postgres-db
    
    # MongoDB Settings (for Vault service)
    MONGO_INITDB_ROOT_USERNAME=mongoadmin
    MONGO_INITDB_ROOT_PASSWORD=mongo_super_secret_password
    MONGO_DB_NAME=securevault_vault_dev
    MONGO_HOST=mongo-db
    
    # RabbitMQ Settings
    RABBITMQ_USER=guest
    RABBITMQ_PASS=guest
    
    # JWT Settings (Should be at least 32 random characters)
    JWT_KEY=ChangeThisToARandomlyGenerated64ByteBase64Key=
    JWT_REFRESH_KEY=AlsoChangeThisToAnotherRandomlyGenerated64ByteBase64Key=
    
    # Seq Settings (Centralized Log Server)
    SEQ_PASSWORD=your_secret_seq_api_key_or_password
    ```

3.  **Launch with Docker:**
    Run the following command in the project's root directory.
    ```bash
    docker-compose up -d --build
    ```

4.  **Check the Status of Services:**
    Ensure all services are in the `Up` or `healthy` state.
    ```bash
    docker-compose ps
    ```

5.  **Run the Application:**
    * **API Gateway:** Accessible at `https://localhost:7202`.
    * **MAUI Client:** Open the `src/clients/SecureVault.App` project in Visual Studio and run it for your desired platform (Windows or Android).

---

### 📄 License

This project is licensed under the [MIT License](https://github.com/kursatabayli/SecureVault/blob/master/LICENSE).
