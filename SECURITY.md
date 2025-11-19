# 🔒 Security Policy

## Supported Versions

Currently supported versions for security updates:

| Version | Supported          |
| ------- | ------------------ |
| Latest (main branch) | ✅ |
| Older releases | ❌ |

## 🚨 Reporting a Vulnerability

**Please DO NOT report security vulnerabilities through public GitHub issues.**

### Reporting Process

1. **Email**: Send details to the repository maintainer
2. **Include**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if available)

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Assessment**: Within 7 days
- **Fix Timeline**: Depends on severity
  - Critical: 1-3 days
  - High: 7-14 days
  - Medium: 14-30 days
  - Low: 30-90 days

### Disclosure Policy

- We will coordinate disclosure timing with you
- Credit will be given for responsible disclosure
- We aim for transparency while protecting users

## 🛡️ Security Considerations

### HashServer Security Model

HashServer is designed for **internal network use** in controlled environments. Consider these aspects:

#### ✅ Security Strengths

- **Cryptographic Integrity**: Uses SHA256 for binary verification
- **No Database Exposure**: No sensitive hash database to protect
- **Read-Only Golden Images**: Server only reads, doesn't modify binaries
- **Stateless Design**: Each request is independent

#### ⚠️ Security Considerations

- **Network Exposure**: Runs HTTP by default (use HTTPS in production)
- **Authentication**: No built-in authentication (use network controls)
- **File System Access**: Requires read access to golden images
- **Internet Fallback**: Optional external API calls

### Production Deployment Best Practices

#### 🌐 Network Security

```json
{
  "App": {
    "Host": {
      // Use HTTPS in production
      "CertificateFile": "production-cert.pfx",
      "CertificatePassword": "strong-password-here"
    },
    "InternalSSL": {
      "gRoot": "https://*:3343/"
    }
  }
}
```

**Recommendations:**
- ✅ Deploy behind firewall
- ✅ Use TLS/SSL certificates
- ✅ Implement network segmentation
- ✅ Use VPN for remote access
- ✅ Monitor access logs
- ❌ Don't expose directly to internet

#### 🔐 Authentication & Authorization

HashServer does not include built-in authentication. Use:

- **Network-level controls**: Firewall rules, VLANs
- **Reverse proxy**: nginx, Apache with authentication
- **API Gateway**: With OAuth2/JWT tokens
- **VPN**: For remote access

**Example nginx configuration:**
```nginx
server {
    listen 443 ssl;
    server_name hashserver.internal;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    # Basic authentication
    auth_basic "HashServer Access";
    auth_basic_user_file /etc/nginx/.htpasswd;
    
    location / {
        proxy_pass http://localhost:3342;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

#### 📁 File System Security

**Golden Images:**
- Store on dedicated, read-only mount
- Use filesystem permissions (read-only for service account)
- Regularly audit for unauthorized changes
- Consider using immutable snapshots

**Cache Files:**
```bash
# Set restrictive permissions
chmod 600 GoldState.buf
chown hashserver:hashserver GoldState.buf
```

#### 🔍 Logging & Monitoring

**Enable appropriate logging:**
```json
{
  "App": {
    "Host": {
      "LogLevel": "Warning"  // Use "Information" for security monitoring
    }
  }
}
```

**Monitor for:**
- Unusual request patterns
- Failed hash validations
- Configuration changes
- File system access anomalies

#### 🌍 External API Calls

When using Internet JITHash fallback:

```json
{
  "App": {
    "Host": {
      // Control external access
      "ProxyToExternalgRoot": false  // Disable if not needed
    },
    "External": {
      "gRoot": "https://pdb2json.azurewebsites.net/"
    }
  }
}
```

**Considerations:**
- ⚠️ External calls may leak metadata
- ⚠️ Dependency on external service availability
- ✅ Use only for well-known Microsoft binaries
- ✅ Consider local-only mode for sensitive environments

#### 🔄 Update Management

**Keep dependencies updated:**
```bash
# Check for outdated packages
dotnet list package --outdated

# Update packages
dotnet add package <PackageName>
```

**Note**: This project uses .NET Core 2.0 (out of support). Consider:
- Upgrading to supported .NET version
- Regular security patches
- Dependency scanning tools

### Secure Configuration Examples

#### Minimal Security (Development)
```json
{
  "App": {
    "Host": {
      "ProxyToExternalgRoot": true,
      "BasePort": 3342
    },
    "Internal": {
      "gRoot": "http://localhost:3342/"
    }
  }
}
```

#### High Security (Production)
```json
{
  "App": {
    "Host": {
      "LogLevel": "Information",
      "CertificateFile": "/secure/path/cert.pfx",
      "CertificatePassword": "strong-password",
      "ProxyToExternalgRoot": false,
      "BasePort": 3343
    },
    "InternalSSL": {
      "gRoot": "https://hashserver.internal:3343/"
    },
    "GoldSourceFiles": {
      "Images": [
        {
          "OS": "Production",
          "ROOT": "/mnt/readonly/golden-images"
        }
      ]
    }
  }
}
```

### Security Checklist

#### Deployment
- [ ] HTTPS enabled with valid certificate
- [ ] Network firewall rules configured
- [ ] Authentication mechanism in place
- [ ] Golden images on read-only filesystem
- [ ] Service runs with minimal permissions
- [ ] Logging configured appropriately
- [ ] External API calls reviewed/disabled

#### Maintenance
- [ ] Regular security updates applied
- [ ] Logs reviewed for anomalies
- [ ] Access controls audited
- [ ] Configuration backed up securely
- [ ] Incident response plan in place

#### Monitoring
- [ ] Failed authentication attempts tracked
- [ ] Unusual request patterns detected
- [ ] File system access monitored
- [ ] Service availability monitored
- [ ] Security logs preserved

## 🔐 Cryptographic Details

### Hash Algorithm

- **Primary**: SHA256
- **Purpose**: Binary integrity verification
- **Collision Resistance**: ~2^256 operations

### Why SHA256?

- ✅ Cryptographically secure
- ✅ Fast computation
- ✅ Wide support
- ✅ No known practical attacks
- ✅ Industry standard

## 🚫 Known Limitations

### Out of Scope

HashServer does **not** protect against:
- ❌ Memory injection attacks (runtime)
- ❌ Rootkits that modify scanning process
- ❌ Hypervisor-level attacks
- ❌ Hardware-based attacks
- ❌ Time-of-check to time-of-use (TOCTOU) issues

### In Scope

HashServer **does** detect:
- ✅ Modified binaries on disk
- ✅ Tampered in-memory code pages
- ✅ Unknown executables
- ✅ Relocated binary differences

## 📚 Security Resources

### General Security
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

### .NET Security
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

### Memory Forensics
- [Volatility Foundation](https://www.volatilityfoundation.org/)
- [Memory Forensics Resources](https://github.com/K2/Scripting)

## 🙏 Acknowledgments

We appreciate security researchers who responsibly disclose vulnerabilities. Contributors will be acknowledged (with permission) in release notes.

## 📞 Contact

For security concerns, please contact the repository maintainers directly rather than opening public issues.

---

<div align="center">

**🔒 Security is a shared responsibility 🔒**

</div>
