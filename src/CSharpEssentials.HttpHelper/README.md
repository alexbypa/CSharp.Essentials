# CSharpEssentials.HttpHelper

![Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-blue)
![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)
![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)
![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.HttpHelper.svg)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)
![GitHub Discussions](https://img.shields.io/github/discussions/alexbypa/CSharp.Essentials)
![Issues](https://img.shields.io/github/issues/alexbypa/CSharp.Essentials)

---

## 📦 Package

### 🛠️ Change Log [Version 4.0.5] 

#### **1. Feature: Custom HttpClient Handler Support**

A significant improvement has been introduced to allow users to inject a custom `SocketsHttpHandler` when configuring a named `HttpClient`. This enhancement provides granular control over network behavior, such as connection settings, proxy configuration, and automatic HTTP compression.

**Example: Enabling Automatic HTTP Compression**

You can now configure automatic HTTP decompression for your named client:

```csharp
var handler = new SocketsHttpHandler 
{
    // Enable automatic decompression for GZip and Deflate
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
};

builder.Services.AddHttpClients(builder.Configuration, handler);
```

`CSharpEssentials.HttpHelper` is a lightweight helper to simplify the usage of `HttpClient` in .NET with a fluent, configurable API.  
It provides convenient methods for request customization, retries, timeouts, authentication headers, and more.


---

## 📖 Documentation & Demo

The full documentation and usage examples (GET requests, retries, authentication, and more) are available here:

👉 [Using HttpHelper – Examples & Demo](https://www.loggerhelper.it)

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!
Feel free to open a [pull request](https://github.com/alexbypa/CSharp.Essentials/pulls) or [issue](https://github.com/alexbypa/CSharp.Essentials/issues).

---

## 📜 License

Distributed under the MIT License. See [LICENSE](../LICENSE) for more information.

```

---