using System;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;
using HotelBooking.Services;

namespace HotelBooking.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly string _path;

    public AuthServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"auth_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private AuthService CreateService()
    {
        return new AuthService(new UserRepository(_path));
    }

    // ---------- Register ----------

    [Fact]
    public void Register_ValidUser_SavesToRepository()
    {
        var service = CreateService();

        service.Register("alice", "qwerty", Role.User);

        var repo = new UserRepository(_path);
        var users = repo.LoadAll(out var skipped);
        Assert.Single(users);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void Register_ReturnsUserWithSameData()
    {
        var service = CreateService();

        var user = service.Register("alice", "qwerty", Role.User);

        Assert.Equal("alice", user.Name);
        Assert.Equal("qwerty", user.Password);
        Assert.Equal(Role.User, user.Role);
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        Assert.Throws<InvalidOperationException>(() =>
            service.Register("alice", "other", Role.Admin));
    }

    [Fact]
    public void Register_DuplicateNameDifferentCase_Ok()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var second = service.Register("Alice", "other", Role.Admin);

        Assert.Equal("Alice", second.Name);
        var repo = new UserRepository(_path);
        var users = repo.LoadAll(out var skipped);
        Assert.Equal(2, users.Count);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void Register_EmptyName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Register("", "qwerty", Role.User));
    }

    [Fact]
    public void Register_WhitespaceName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Register("   ", "qwerty", Role.User));
    }

    [Fact]
    public void Register_EmptyPassword_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Register("alice", "", Role.User));
    }

    [Fact]
    public void Register_AdminRole_SavesAdmin()
    {
        var service = CreateService();

        service.Register("root", "secret", Role.Admin);

        var repo = new UserRepository(_path);
        var users = repo.LoadAll(out _);
        Assert.Single(users);
        Assert.Equal(Role.Admin, users[0].Role);
    }

    // ---------- TryLogin ----------

    [Fact]
    public void TryLogin_ValidCredentials_ReturnsTrueAndUser()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("alice", "qwerty", out var user);

        Assert.True(ok);
        Assert.Equal("alice", user.Name);
        Assert.Equal(Role.User, user.Role);
    }

    [Fact]
    public void TryLogin_WrongPassword_ReturnsFalse()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("alice", "wrong", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryLogin_UnknownName_ReturnsFalse()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("bob", "qwerty", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryLogin_EmptyName_ReturnsFalse()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("", "qwerty", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryLogin_EmptyPassword_ReturnsFalse()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("alice", "", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryLogin_CaseSensitiveName_ReturnsFalse()
    {
        var service = CreateService();
        service.Register("alice", "qwerty", Role.User);

        var ok = service.TryLogin("Alice", "qwerty", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryLogin_ReturnsUserWithCorrectRole()
    {
        var service = CreateService();
        service.Register("root", "secret", Role.Admin);

        var ok = service.TryLogin("root", "secret", out var user);

        Assert.True(ok);
        Assert.Equal(Role.Admin, user.Role);
    }

    // ---------- Integration ----------

    [Fact]
    public void Register_ThenTryLogin_Works()
    {
        var service = CreateService();

        service.Register("alice", "qwerty", Role.User);
        var ok = service.TryLogin("alice", "qwerty", out var user);

        Assert.True(ok);
        Assert.Equal("alice", user.Name);
        Assert.Equal(Role.User, user.Role);
    }
}