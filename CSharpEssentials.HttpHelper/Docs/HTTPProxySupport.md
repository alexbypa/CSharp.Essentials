# 🧩 HTTP Proxy Support

This guide demonstrates how to verify that **CSharpEssentials.HttpHelper** correctly uses an HTTP proxy by setting up **Squid proxy in Docker** and monitoring real traffic.

---

## 📋 Prerequisites

- Docker Desktop installed
- .NET 8+ SDK
- Basic understanding of HTTP proxies

---

## 🚀 Quick Setup

### Step 1: Start Squid Container

Run Squid in Docker with the custom configuration:

```bash
docker run -d -p 8888:3128 --name squid-proxy ubuntu/squid
```

**Verify it's running:**

```bash
docker ps | grep squid-proxy
```

---

### Step 2: Configure Your Application

Update your `appsettings.json` or `appsettings.Development.json` (section httpProxy) on your array item of HttpClientOptions :

```json
"HttpClientOptions": [
    {
      "name": "testAI",
      "certificate": {
        "path": "",
        "password": ""
      },
      "httpProxy": {
        "Address": "http://127.0.0.1:8888",
        "UserName": "alex",
        "Password": "ciccio",
        "UseProxy": true
      },
      "RateLimitOptions": {
        "AutoReplenishment": true,
        "PermitLimit": 1,
        "QueueLimit": 1,
        "Window": "00:00:15",
        "SegmentsPerWindow": 2,
        "IsEnabled": false
      }
    },....
```

---

### Step 3: Register Services

In your `Program.cs`:

```csharp
using CSharpEssentials.HttpHelper.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register HttpHelper with proxy configuration
builder.Services.AddHttpClients(builder.Configuration);

var app = builder.Build();

// Your endpoints here...

app.Run();
```

---

### Step 4: Create Test Endpoint

Add a minimal API endpoint to test the proxy:

```csharp
app.MapGet("Test/proxyweb", async (IhttpsClientHelperFactory httpFactory, string httpOptionName = "testAI") => {
		string url = "https://example.com/";
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
		return Results.Ok(await response.Content.ReadAsStringAsync());
})
.WithTags("HTTP HELPER")
.WithSummary("proxyweb");
```

---

## 🔍 Verify Proxy Usage

### Monitor Squid Logs in Real-Time

Open a terminal and run:

```bash
docker logs squid-proxy -f
```

### Make a Request

Call your endpoint:

```bash
curl http://localhost:1234/scalar/#tag/http-helper/get/Test/proxyweb
```

Or using your browser/Postman.

---

### Expected Squid Log Output

You should see a log entry like this:

```
1764509475.538  83259 172.17.0.1 TCP_TUNNEL/200 2235 CONNECT example.com:443 - HIER_DIRECT/23.192.228.80 -
```

## 🧪 Test Without Proxy

### Disable Proxy

Update `appsettings.json`:

```json
      "httpProxy": {
        "Address": "http://127.0.0.1:8888",
        "UserName": "alex",
        "Password": "ciccio",
        "UseProxy": false
      },
```

### Make Another Request

```bash
curl http://localhost:1234/scalar/#tag/http-helper/get/Test/proxyweb
```

### Check Squid Logs

```bash
docker logs squid-proxy -f
```

**Expected result:** ❌ **No new log entries** (traffic bypassed the proxy)

---

## 📊 Comparison

| Configuration | `UseProxy` | Squid Logs | Result |
|--------------|------------|------------|--------|
| **With Proxy** | `true` | `TCP_TUNNEL/200 CONNECT example.com:443` | ✅ Traffic through proxy |
| **Without Proxy** | `false` | *(no logs)* | ✅ Direct connection |

---

## 🛠️ Troubleshooting

### Issue: "502 Upstream error"

**Possible causes:**

1. **Timeout too low** - Increase `TimeoutSeconds` in config
2. **Squid not running** - Check with `docker ps`
3. **Wrong proxy address** - Verify `127.0.0.1:8888` is correct
4. **Port conflict** - Check if port 8888 is already in use:
   ```bash
   netstat -ano | findstr :8888
   ```

### Issue: No logs in Squid

**Solution:**

1. Restart container:
   ```bash
   docker restart squid-proxy
   ```

2. Check Squid is accepting connections:
   ```bash
   docker logs squid-proxy | grep "Accepting HTTP Socket"
   ```

3. Verify configuration is mounted:
   ```bash
   docker exec squid-proxy cat /etc/squid/squid.conf
   ```

---

## 🧹 Cleanup

Remove the Squid container when done:

```bash
docker stop squid-proxy
docker rm squid-proxy
```

---

## 📚 Understanding Squid Logs

### Common Log Codes

| Code | Meaning |
|------|---------|
| `TCP_TUNNEL/200` | ✅ HTTPS tunnel successful |
| `TCP_MISS/200` | ✅ HTTP request successful (not cached) |
| `TCP_HIT/200` | ✅ Response served from cache |
| `TCP_DENIED/403` | ❌ Request blocked by ACL |
| `NONE/502` | ❌ Connection to target failed |

### Log Format

```
timestamp duration client_ip result/code bytes method URL - hierarchy/ip -
```

**Reference:** [Squid Log Format Documentation](https://wiki.squid-cache.org/SquidFaq/SquidLogs)

---

## 🎯 Production Considerations

### Security

⚠️ The configuration above allows **all traffic** for testing purposes.

For production, restrict access:

```conf
# Only allow specific networks
acl localnet src 10.0.0.0/8
acl localnet src 172.16.0.0/12
acl localnet src 192.168.0.0/16

http_access allow localnet
http_access deny all
```

### Authentication

Add basic authentication:

```conf
auth_param basic program /usr/lib/squid/basic_ncsa_auth /etc/squid/passwords
auth_param basic realm Squid Proxy
acl authenticated proxy_auth REQUIRED
http_access allow authenticated
```

### Caching

Enable caching for better performance:

```conf
# Enable disk cache
cache_dir ufs /var/spool/squid 100 16 256

# Allow caching
cache allow all
```

**Result:** You can now confidently verify that your application uses HTTP proxies correctly! 🎉

---

**Questions or issues?** Open an issue on [GitHub](https://github.com/alexbypa/CSharp.Essentials/issues)!