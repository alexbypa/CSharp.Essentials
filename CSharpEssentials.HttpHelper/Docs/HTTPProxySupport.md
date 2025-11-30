# 🧩 HTTP Proxy Support

`CSharpEssentials.HttpHelper` provides **built-in HTTP proxy support** with automatic configuration from `appsettings.json`.

## 📦 Quick Setup

### 1️⃣ Install the package

```bash
dotnet add package CSharpEssentials.HttpHelper
```

### 2️⃣ Register services

```csharp
builder.Services.AddHttpClients(builder.Configuration);
```

### 3️⃣ Configure proxy in `appsettings.json`

```json
{
  "HttpClientOptions": {
    "default": {
      "HttpProxy": {
        "UseProxy": true,
        "Address": "127.0.0.1:8888",
        "UserName": "alex",
        "Password": "ciccio"
      }
    }
  }
}
```

> 💡 **Tip**: Set `UseProxy: false` to disable proxy for specific clients.

---

## 🧪 Testing with Fiddler

### Step 1: Create a test endpoint

```csharp
app.MapGet("Test/proxyweb", async (IhttpsClientHelperFactory httpFactory, string httpOptionName = "testAI") => {
    string url = "https://httpbin.org/get";
    var client = httpFactory.CreateOrGet(httpOptionName);

    IContentBuilder nobody = new NoBodyContentBuilder();
    CancellationTokenSource cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(30));

    var response = await client.SendAsync(url, HttpMethod.Get, null, nobody, null, cts.Token);
    try {
        response.EnsureSuccessStatusCode();
    } catch (HttpRequestException ex) {
        return Results.Problem($"[FAIL HTTP] connection Error on Proxy/Target: {ex.Message}.");
    } catch (UriFormatException ex) {
        return Results.Problem($"[FAIL HTTP] URI fomat error on configuration: {ex.Message}.");
    }
    return Results.Ok("See console output for how to call HttpHelper with actions.");
})
```

---

### Step 2: Configure Fiddler as mock server

1. Open **Fiddler Classic**
2. Go to **AutoResponder** tab
3. Enable: ☑ **Enable rules** + ☑ **Unmatched requests passthrough**
4. Add a new rule:

| **Match Rule** | **Response** |
|----------------|--------------|
| `EXACT:http://localhost:1234/Test/proxyweb` | `D:\fiddlertest.json` |

**Content of `fiddlertest.json`**:
```json
{
  "proxy": "Fiddler",
  "intercepted": true
}
```

---

### Step 3: Call your endpoint

```bash
GET http://localhost:1234/Test/proxyweb?httpOptionName=default
```

---

### Step 4: Verify in Fiddler

If the proxy is correctly configured, you'll see:

#### ✅ In Fiddler's **Inspectors** tab:

**Request captured:**
```
GET http://localhost:1234/Test/proxyweb?httpOptionName=testAI HTTP/1.1
Host: localhost:1234
```

**Response returned (from mock file):**
```json
{
  "proxy": "Fiddler",
  "intercepted": true
}
```

#### ✅ In your application logs:

```
[200] GET http://localhost:1234/Test/proxyweb
```

#### ✅ In your API response:

```json
{
  "proxy": "Fiddler",
  "intercepted": true
}
```

---

## 🎯 What This Proves

| Evidence | Meaning |
|----------|---------|
| **Fiddler shows the request** | ✅ Traffic is routed through proxy (127.0.0.1:8888) |
| **Mock response is returned** | ✅ AutoResponder intercepted the request |
| **Status 200 in logs** | ✅ HttpHelper successfully handled the response |

> 🔐 **HTTPS Support**: Enable `Tools → Options → HTTPS → Decrypt HTTPS traffic` to inspect encrypted requests.

---

## 🛠️ Common Issues

### ❌ `502 Bad Gateway` error

**Cause**: Proxy address is incorrect or proxy server is offline.

**Solution**:
1. Verify proxy is running (e.g., Fiddler on port 8888)
2. Check `appsettings.json` has correct `Address`
3. Test with `UseProxy: false` to bypass proxy

---

### ❌ Request not appearing in Fiddler

**Cause**: Application is not using the configured proxy.

**Solution**:
1. Ensure `AddHttpClients(configuration)` is called in `Program.cs`
2. Verify `httpOptionName` parameter matches a configured client
3. Check Fiddler is capturing traffic (`File → Capture Traffic` enabled)

---

## 📚 Advanced Configuration

### Per-client proxy settings

```json
{
  "HttpClientOptions": {
    "externalApi": {
      "BaseAddress": "https://api.example.com",
      "HttpProxy": {
        "UseProxy": true,
        "Address": "corporate-proxy.local:8080",
        "BypassOnLocal": true
      }
    },
    "internalApi": {
      "BaseAddress": "https://internal.company.com",
      "HttpProxy": {
        "UseProxy": false  // Direct connection
      }
    }
  }
}
```

---

## 🎬 Demo Screenshot

![Fiddler AutoResponder in action](fiddler-demo.png)

*Example: Fiddler intercepting a request and returning a mocked JSON response through the configured proxy.*

---

## 📖 Related Documentation

- [Getting Started](./README.md#getting-started)
- [Request/Response Actions](./README.md#requestresponse-actions)
- [Error Handling](./README.md#error-handling)

---

**🎉 You're all set!** Your application now supports proxy configuration with zero code changes. Just update `appsettings.json` and go! 🚀

---