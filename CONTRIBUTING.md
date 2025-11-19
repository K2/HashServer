# 🤝 Contributing to HashServer

Thank you for your interest in contributing to HashServer! This document provides guidelines for contributing to the project.

---

## 📋 Table of Contents

- [Code of Conduct](#-code-of-conduct)
- [How Can I Contribute?](#-how-can-i-contribute)
- [Development Setup](#-development-setup)
- [Pull Request Process](#-pull-request-process)
- [Coding Standards](#-coding-standards)
- [Testing Guidelines](#-testing-guidelines)
- [Documentation](#-documentation)

---

## 📜 Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in all interactions.

### Expected Behavior

- ✅ Use welcoming and inclusive language
- ✅ Be respectful of differing viewpoints
- ✅ Accept constructive criticism gracefully
- ✅ Focus on what is best for the community
- ✅ Show empathy towards others

### Unacceptable Behavior

- ❌ Harassment or discriminatory language
- ❌ Trolling or insulting comments
- ❌ Personal or political attacks
- ❌ Publishing others' private information

---

## 🎯 How Can I Contribute?

### 🐛 Reporting Bugs

**Before submitting a bug report:**
1. Check existing [GitHub Issues](https://github.com/K2/HashServer/issues)
2. Update to the latest version
3. Gather relevant information

**When reporting bugs, include:**
- Clear, descriptive title
- Steps to reproduce
- Expected vs. actual behavior
- Environment details (.NET version, OS, etc.)
- Configuration (sanitized - remove sensitive data)
- Error messages and stack traces
- Screenshots if applicable

**Template:**
```markdown
**Environment:**
- OS: [e.g., Windows 10, Ubuntu 20.04]
- .NET Version: [e.g., .NET Core 2.0]
- HashServer Version: [e.g., commit hash or release]

**Steps to Reproduce:**
1. Configure with...
2. Run command...
3. Observe error...

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happens]

**Error Messages:**
```
[Paste error messages here]
```

**Additional Context:**
[Any other relevant information]
```

### 💡 Suggesting Enhancements

**Enhancement suggestions should include:**
- Clear use case
- Why it benefits the community
- Proposed implementation (if applicable)
- Potential alternatives considered

**Label your issue:** `enhancement`

### 📝 Documentation Improvements

Documentation is crucial! You can help by:
- Fixing typos or unclear sections
- Adding examples
- Improving diagrams
- Translating documentation
- Creating tutorials or guides

### 💻 Code Contributions

Areas where contributions are welcome:
- 🐛 Bug fixes
- ✨ New features (discuss first via issue)
- ⚡ Performance improvements
- 🧪 Test coverage
- 📖 Code documentation
- 🔒 Security enhancements

---

## 🛠️ Development Setup

### Prerequisites

- .NET Core 2.0 SDK or higher
- Git
- Text editor or IDE (Visual Studio, VS Code, JetBrains Rider)

### Fork and Clone

```bash
# Fork the repository on GitHub, then:
git clone https://github.com/YOUR_USERNAME/HashServer.git
cd HashServer

# Add upstream remote
git remote add upstream https://github.com/K2/HashServer.git
```

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests (if available)
dotnet test

# Run the server
dotnet run
```

### Create a Branch

```bash
# Update your fork
git checkout main
git pull upstream main

# Create feature branch
git checkout -b feature/your-feature-name
```

---

## 🔄 Pull Request Process

### Before Submitting

- [ ] Code follows project style guidelines
- [ ] All tests pass (if applicable)
- [ ] Documentation updated (if applicable)
- [ ] Commit messages are clear and descriptive
- [ ] No merge conflicts with main branch

### PR Template

```markdown
## Description
[Brief description of changes]

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
[How was this tested?]

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests added/updated (if applicable)

## Related Issues
Fixes #[issue_number]
```

### Review Process

1. **Submit PR**: Provide clear description
2. **CI Checks**: Automated checks must pass
3. **Code Review**: Maintainers will review
4. **Address Feedback**: Make requested changes
5. **Approval**: Once approved, will be merged
6. **Merge**: Maintainer will merge

### After Merge

```bash
# Update your fork
git checkout main
git pull upstream main
git push origin main

# Delete feature branch
git branch -d feature/your-feature-name
git push origin --delete feature/your-feature-name
```

---

## 📐 Coding Standards

### C# Style Guide

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Use meaningful variable names
- Keep methods focused and short
- Comment complex logic
- Use async/await properly

### Example

```csharp
// ✅ Good
public async Task<HashResult> ValidateHashAsync(HashRequest request)
{
    if (request == null)
        throw new ArgumentNullException(nameof(request));
    
    // Check local cache first
    var cachedResult = await _cache.GetAsync(request.Hash);
    if (cachedResult != null)
        return cachedResult;
    
    // JIT hash calculation
    var result = await CalculateJitHashAsync(request);
    await _cache.SetAsync(request.Hash, result);
    
    return result;
}

// ❌ Avoid
public HashResult VH(HashRequest r)
{
    var c = _cache.Get(r.Hash);
    if (c != null) return c;
    var res = CalcHash(r);
    _cache.Set(r.Hash, res);
    return res;
}
```

### Git Commit Messages

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit first line to 72 characters
- Reference issues and PRs

**Examples:**
```
✅ Add JIT hash caching for improved performance
✅ Fix null reference exception in hash validation
✅ Update README with installation instructions
✅ Refactor PageHash API endpoint (#123)

❌ fixed stuff
❌ WIP
❌ asdf
```

---

## 🧪 Testing Guidelines

### Writing Tests

- Write unit tests for new features
- Maintain or improve code coverage
- Test edge cases and error conditions
- Use descriptive test names

### Test Structure

```csharp
[Fact]
public async Task ValidateHash_WithValidInput_ReturnsKnownResult()
{
    // Arrange
    var hashServer = new HashServer(config);
    var request = new HashRequest 
    { 
        Hash = "valid_sha256_hash",
        FileName = "kernel32.dll"
    };
    
    // Act
    var result = await hashServer.ValidateHashAsync(request);
    
    // Assert
    Assert.True(result.IsKnown);
    Assert.Equal("Local", result.Source);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter FullyQualifiedName~TestNamespace.TestClass.TestMethod

# With coverage (if configured)
dotnet test /p:CollectCoverage=true
```

---

## 📖 Documentation

### Code Documentation

- Document public APIs with XML comments
- Explain complex algorithms
- Include usage examples

```csharp
/// <summary>
/// Validates a memory hash against known-good binaries using JIT calculation.
/// </summary>
/// <param name="request">Hash validation request containing SHA256 and metadata</param>
/// <returns>Result indicating if hash is known and from which source</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null</exception>
/// <example>
/// <code>
/// var result = await ValidateHashAsync(new HashRequest 
/// { 
///     Hash = "abc123...",
///     FileName = "kernel32.dll"
/// });
/// </code>
/// </example>
public async Task<HashResult> ValidateHashAsync(HashRequest request)
{
    // Implementation
}
```

### Markdown Documentation

- Use proper markdown formatting
- Include code examples
- Add diagrams where helpful
- Keep language clear and concise

---

## 🎓 Resources

### Project Resources
- [Main Documentation](README.md)
- [Technical Workflows](docs/HashServer.md)
- [Scripting Examples](https://github.com/K2/Scripting)
- [inVtero.net](https://github.com/K2/inVtero)

### Learning Resources
- [.NET Core Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

---

## ❓ Questions?

- 💬 Open a [GitHub Discussion](https://github.com/K2/HashServer/discussions)
- 🐛 File an [Issue](https://github.com/K2/HashServer/issues)
- 📧 Contact maintainers (see repository)

---

## 🙏 Thank You!

Your contributions help make HashServer better for everyone. We appreciate your time and effort!

---

<div align="center">

**⭐ Don't forget to star the repository! ⭐**

</div>
