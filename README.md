# 🗃️ MemeStash

**Your personal meme vault — store, browse, and share memes from a simple web UI backed by Azure Blob Storage.**

MemeStash is a lightweight ASP.NET Core web application that lets you create a personal stash using a short URL slug, upload memes to it, and grab direct image links for easy sharing. No accounts, no databases — just a slug and a blob container.

---

## ✨ Features

- **Personal stashes** — Create a stash with any URL slug (e.g., `/stash/dank`). No signup required.
- **Upload memes** — Drag-and-drop or select JPG, PNG, GIF, and WebP files (up to 5 MB each).
- **Browse & share** — View your stash as a gallery and copy direct image links with one click.
- **Delete memes** — Remove anything you no longer need.
- **Rate limiting** — Built-in fixed-window rate limiting on uploads to prevent abuse.
- **Minimal footprint** — No database. Azure Blob Storage handles all persistence.

---

## 🏗️ Architecture

```
┌────────────────────────┐
│    Razor Pages UI      │  ← HTML / JS / CSS frontend
├────────────────────────┤
│   Minimal API          │  ← Upload, list, delete endpoints
│   Endpoints/           │     with rate limiting middleware
├────────────────────────┤
│   BlobStorageService   │  ← Implements IMemeStorageService
│   Services/            │
├────────────────────────┤
│   Azure Blob Storage   │  ← Meme file persistence
└────────────────────────┘
```

Memes are stored in Azure Blob Storage, organized by stash slug. The application uses ASP.NET Core Razor Pages for the UI and minimal API endpoints for file operations. A `BlobStorageService` implements the `IMemeStorageService` interface, making the storage backend swappable for testing or alternative providers.

---

## 📁 Project Structure

```
MemeStash/
├── Endpoints/           # Minimal API endpoint definitions (upload, list, delete)
├── Models/              # Data models and configuration options (AzureStorageOptions, etc.)
├── Pages/               # Razor Pages (.cshtml + .cshtml.cs)
├── Properties/          # Launch settings
├── Services/            # BlobStorageService and IMemeStorageService interface
├── wwwroot/             # Static assets (CSS, JavaScript, images)
├── Program.cs           # Application entry point, DI setup, middleware pipeline
├── appsettings.json     # Configuration (Azure Storage connection string, etc.)
├── MemeStash.csproj     # Project file
└── memestash.sln        # Solution file
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or whichever version the `.csproj` targets)
- An Azure Storage account **or** [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for local emulation

### 1. Clone the repository

```bash
git clone https://github.com/jussiroine/memestash.git
cd memestash
```

### 2. Configure Azure Storage

Add your connection string to `appsettings.json` (or better, use User Secrets or environment variables):

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  }
}
```

> **Using Azurite locally?** Install it via npm (`npm install -g azurite`) or use the VS Code extension, then start it with `azurite --silent`. The default connection string `UseDevelopmentStorage=true` will work out of the box.

To use **User Secrets** instead of putting the connection string in config files:

```bash
dotnet user-secrets init
dotnet user-secrets set "AzureStorage:ConnectionString" "<your-connection-string>"
```

### 3. Run the application

```bash
dotnet run --project MemeStash.csproj
```

The app will start on `https://localhost:5001` (or the port configured in `Properties/launchSettings.json`).

### 4. Open your stash

Navigate to `https://localhost:5001` and create or visit a stash by its slug.

---

## ⚙️ Configuration

Configuration is bound to the `AzureStorageOptions` class via the `AzureStorage` section in `appsettings.json`.

| Setting | Description | Default |
|---|---|---|
| `AzureStorage:ConnectionString` | Azure Blob Storage connection string | — |

### Upload Limits

- **Max file size:** 5 MB per upload (Kestrel is configured to accept up to 6 MB to account for multipart form overhead)
- **Accepted formats:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`
- **Rate limiting:** Fixed-window rate limiting is applied to the upload endpoint to prevent abuse

---

## 🧰 Tech Stack

| Layer | Technology |
|---|---|
| **Web framework** | ASP.NET Core (Razor Pages + Minimal APIs) |
| **Storage** | Azure Blob Storage |
| **Rate limiting** | `System.Threading.RateLimiting` (built-in ASP.NET Core) |
| **Frontend** | Vanilla HTML, CSS, and JavaScript |

---

## 🧪 Development Tips

### Use Azurite for local development

Azurite is the recommended local emulator for Azure Storage. It's fast, free, and doesn't require an Azure subscription:

```bash
# Install
npm install -g azurite

# Start (in a separate terminal)
azurite --silent --location ./azurite-data
```

### Swap the storage backend

`BlobStorageService` implements the `IMemeStorageService` interface. You can create an alternative implementation (e.g., local file system, S3) and register it in `Program.cs` without touching the rest of the application.

---

## 🚢 Deployment

MemeStash is a standard ASP.NET Core application and can be deployed to any environment that supports it. Some natural fits:

- **Azure App Service** — Publish directly from Visual Studio, `dotnet publish`, or GitHub Actions.
- **Azure Container Apps** — Containerize with `docker build` and deploy.
- **Any Linux/Windows server** — Self-host with Kestrel behind a reverse proxy (nginx, Caddy, etc.).

Make sure the `AzureStorage:ConnectionString` environment variable or app setting points to your production storage account.

---

## 🤝 Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/awesome-thing`)
3. Commit your changes (`git commit -m 'Add awesome thing'`)
4. Push to the branch (`git push origin feature/awesome-thing`)
5. Open a Pull Request

---

## 📄 License

This project does not currently specify a license. Contact the repository owner for usage terms.
