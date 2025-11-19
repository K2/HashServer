# 📋 HashServer Quick Reference

> **💡 Quick access guide** for common tasks and configurations

---

## 🚀 Quick Start Commands

### Installation
```bash
git clone https://github.com/K2/HashServer.git
cd HashServer
dotnet restore
dotnet build
dotnet run
```

### Basic Test
```powershell
# Download PowerShell client
iwr https://raw.githubusercontent.com/K2/Scripting/master/Test-AllVirtualMemory.ps1 -OutFile Test.ps1

# Configure endpoint
$HashServerUri = "http://localhost:3342/api/PageHash/x"

# Run scan
.\Test.ps1
```

---

## ⚙️ Configuration Quick Reference

### Minimal Configuration
```json
{
  "App": {
    "Host": {
      "BasePort": 3342,
      "ProxyToExternalgRoot": true
    },
    "GoldSourceFiles": {
      "Images": [
        {
          "OS": "Win10",
          "ROOT": "C:\\GoldenImages\\Win10"
        }
      ]
    }
  }
}
```

### Production Configuration
```json
{
  "App": {
    "Host": {
      "LogLevel": "Warning",
      "CertificateFile": "cert.pfx",
      "CertificatePassword": "password",
      "ThreadCount": 128,
      "ProxyToExternalgRoot": false,
      "BasePort": 3343
    },
    "InternalSSL": {
      "gRoot": "https://*:3343/"
    },
    "GoldSourceFiles": {
      "Images": [
        {
          "OS": "Production",
          "ROOT": "/mnt/golden-images"
        }
      ]
    }
  }
}
```

---

## 🔌 API Quick Reference

### Endpoint
```
POST /api/PageHash/x
Content-Type: application/json
```

### Request
```json
{
  "Hash": "sha256_hash_value",
  "Size": 4096,
  "TimeStamp": 1234567890,
  "VirtualSize": 4096,
  "FileName": "kernel32.dll",
  "PDB": "kernel32.pdb",
  "Characteristics": 536870944
}
```

### Response
```json
{
  "IsKnown": true,
  "Source": "Local",
  "MatchedFile": "C:\\Windows\\System32\\kernel32.dll",
  "Confidence": 100
}
```

### cURL Example
```bash
curl -X POST http://localhost:3342/api/PageHash/x \
  -H "Content-Type: application/json" \
  -d '{
    "Hash": "abc123...",
    "FileName": "kernel32.dll",
    "Size": 4096
  }'
```

---

## 🛠️ Common Tasks

### Update Golden Images
```bash
# 1. Update files in ROOT directory
cp -r /new/images/* /mnt/golden-images/

# 2. Delete cache file
rm GoldState.buf

# 3. Restart server
dotnet run
```

### Check Server Status
```bash
# Check if running
netstat -an | grep 3342

# Test endpoint
curl http://localhost:3342/api/PageHash/x

# View logs (if configured)
tail -f hashserver.log
```

### Enable HTTPS
```bash
# Generate test certificate
dotnet dev-certs https -ep testCert.pfx -p testPassword

# Update appsettings.json
{
  "CertificateFile": "testCert.pfx",
  "CertificatePassword": "testPassword"
}
```

---

## 🔍 Troubleshooting Quick Fixes

### Server Won't Start
```bash
# Check port availability
netstat -an | grep 3342

# Check .NET version
dotnet --version

# Restore packages
dotnet restore

# Clean build
dotnet clean && dotnet build
```

### Low Hit Rate
```json
// Enable Internet fallback
{
  "ProxyToExternalgRoot": true
}
```

### Slow Performance
```json
// Increase threads
{
  "ThreadCount": 256,
  "MaxConcurrentConnections": 8192
}
```

### Cache Issues
```bash
# Clear cache
rm GoldState.buf

# Restart server
dotnet run
```

---

## 📊 Performance Tuning

### Server-Side
```json
{
  "ThreadCount": 128,           // CPU cores × 8-16
  "MaxConcurrentConnections": 4096,
  "FileLocateNfo": "GoldState.buf"  // Enable caching
}
```

### Client-Side
```powershell
# Scan only working set (faster)
$ScanWorkingSetOnly = $true

# Use parallel processing
$MaxParallelScans = 10

# Enable local caching
$EnableClientCache = $true
```

---

## 🔐 Security Checklist

- [ ] **HTTPS enabled** in production
- [ ] **Firewall rules** configured
- [ ] **Authentication** via reverse proxy
- [ ] **Read-only** golden images
- [ ] **Minimal permissions** for service account
- [ ] **Logging enabled** and monitored
- [ ] **External API** reviewed (ProxyToExternalgRoot)

---

## 📦 File Structure

```
HashServer/
├── README.md              # Main documentation
├── CONTRIBUTING.md        # Contribution guidelines
├── SECURITY.md           # Security policy
├── docs/
│   └── HashServer.md     # Technical documentation
├── appsettings.json      # Configuration
├── Program.cs            # Entry point
├── Startup.cs            # ASP.NET setup
├── WebAPI.cs             # API endpoints
├── PageHash.cs           # Hash validation logic
├── GoldImages.cs         # Golden image management
└── Settings.cs           # Configuration models
```

---

## 🌐 Important URLs

| Resource | URL |
|----------|-----|
| **Repository** | https://github.com/K2/HashServer |
| **Client Scripts** | https://github.com/K2/Scripting |
| **Public API** | https://pdb2json.azurewebsites.net/ |
| **Issues** | https://github.com/K2/HashServer/issues |
| **Discussions** | https://github.com/K2/HashServer/discussions |
| **inVtero.net** | https://github.com/K2/inVtero |

---

## 💻 PowerShell Client Configuration

### Minimal
```powershell
$HashServerUri = "http://localhost:3342/api/PageHash/x"
.\Test-AllVirtualMemory.ps1
```

### With Fallback
```powershell
$gRoot = "https://pdb2json.azurewebsites.net/api/PageHash/x"
$HashServerUri = "http://10.0.0.118:3342/api/PageHash/x"
.\Test-AllVirtualMemory.ps1
```

### Advanced Options
```powershell
# Scan specific process
$ProcessName = "explorer.exe"

# Scan all memory (slower)
$ScanAllMemory = $true

# Increase parallel jobs
$ThrottleLimit = 20

# Enable verbose output
$VerbosePreference = "Continue"

.\Test-AllVirtualMemory.ps1
```

---

## 🎯 Use Case Quick Reference

### 🔍 Forensic Analysis
- Analyze memory dumps with Volatility plugin
- Identify unknown code in static images
- Generate verification reports

### 🚨 Incident Response
- Real-time memory scanning of live systems
- Detect modified/malicious binaries
- Rapid triage of compromised systems

### 🛡️ Intrusion Detection
- Continuous memory integrity monitoring
- Baseline establishment for known-good state
- Alerting on unknown code execution

---

## 📞 Getting Help

| Issue Type | Contact Method |
|------------|----------------|
| **Bug Report** | [GitHub Issues](https://github.com/K2/HashServer/issues) |
| **Feature Request** | [GitHub Issues](https://github.com/K2/HashServer/issues) (label: enhancement) |
| **Question** | [GitHub Discussions](https://github.com/K2/HashServer/discussions) |
| **Security** | See [SECURITY.md](SECURITY.md) |
| **Contributing** | See [CONTRIBUTING.md](CONTRIBUTING.md) |

---

## 🔄 Common Workflows

### Setup New Environment
```bash
# 1. Install prerequisites
dotnet --version  # Verify .NET installed

# 2. Clone and build
git clone https://github.com/K2/HashServer.git
cd HashServer
dotnet restore && dotnet build

# 3. Configure
cp appsettings.json appsettings.Production.json
# Edit appsettings.Production.json

# 4. Prepare golden images
mkdir /mnt/golden-images
# Copy known-good binaries

# 5. Run
dotnet run --configuration Production
```

### Regular Maintenance
```bash
# Weekly: Check for updates
git pull
dotnet restore
dotnet build

# Monthly: Update golden images
# (when new patches/software deployed)

# As needed: Clear cache
rm GoldState.buf && dotnet run
```

---

<div align="center">

**📖 [Full Documentation](README.md)** | **🔧 [Technical Details](docs/HashServer.md)** | **🤝 [Contributing](CONTRIBUTING.md)**

</div>
