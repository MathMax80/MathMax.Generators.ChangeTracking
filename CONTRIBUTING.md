# Contributing to MathMax.Generators.ChangeTracking

First off, thank you for considering contributing to **MathMax.Generators.ChangeTracking** 🎉  
Contributions are what make open source such a great place to learn, inspire, and build.

---

## 📋 How to Contribute

We accept contributions via **forks** and **pull requests**.  

1. **Fork the repository** (click the "Fork" button on GitHub).  
2. **Clone your fork**:
   ```bash
   git clone https://github.com/<your-username>/MathMax.Generators.ChangeTracking.git
   cd MathMax.Generators.ChangeTracking
   ```
3. **Add the upstream remote**:
   ```bash
   git remote add upstream https://github.com/<original-owner>/MathMax.Generators.ChangeTracking.git
   ```
4. **Create a branch** for your feature or fix:
   ```bash
   git checkout -b feature/my-awesome-change
   ```
5. **Make your changes** (see [Development](#-development) below).  
6. **Commit** your work:
   ```bash
   git commit -m "Add: description of your change"
   ```
7. **Push** your branch:
   ```bash
   git push origin feature/my-awesome-change
   ```
8. **Open a Pull Request** on GitHub against the `main` branch.

---

## 💻 Development

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer  
- An IDE (Visual Studio, Rider, or VS Code)  

### Build and Test
```bash
dotnet restore
dotnet build
dotnet test
```

- Source code lives in **`src/`**  
- Tests live in **`tests/`**  
- Please add or update **unit tests** for any changes  

---

## ✅ Pull Request Guidelines

- Target the **`main`** branch.  
- Keep PRs small and focused on a single change.  
- Ensure **all tests pass** (`dotnet test`).  
- Follow the existing code style (`.editorconfig` enforces formatting).  
- If your change affects public APIs, update XML docs and/or README.  

---

## 📦 Release Process

- The `main` branch is always **stable and release-ready**.  
- New releases are published when a **tag** (`vX.Y.Z`) is created on `main`.  
- GitHub Actions automatically builds and publishes the NuGet package.  

---

## 📝 Code of Conduct

By participating, you agree to follow our [Code of Conduct](CODE_OF_CONDUCT.md).

---

🙌 Thanks for helping make **MathMax.Generators.ChangeTracking** better!

