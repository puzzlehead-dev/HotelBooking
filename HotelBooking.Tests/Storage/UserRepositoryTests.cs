using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;

namespace HotelBooking.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly string _path;

    public UserRepositoryTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"users_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private void WriteFile(params string[] lines) => File.WriteAllLines(_path, lines);
    private string[] ReadFile() => File.ReadAllLines(_path);

    [Fact]
    public void LoadAll_FileNotExists_ReturnsEmptyListAndZeroSkipped()
    {
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_EmptyFile_ReturnsEmptyListAndZeroSkipped()
    {
        WriteFile();
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_OneValidLine_ReturnsOneUser()
    {
        WriteFile("alice|qwerty|User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Single(users);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("qwerty", users[0].Password);
        Assert.Equal(Role.User, users[0].Role);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_OneValidAdminLine_ReturnsAdmin()
    {
        WriteFile("root|secret|Admin");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Single(users);
        Assert.Equal(Role.Admin, users[0].Role);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithTwoFields_SkipsLine()
    {
        WriteFile("alice|qwerty");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithFourFields_SkipsLine()
    {
        WriteFile("alice|qwerty|User|extra");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEmptyName_SkipsLine()
    {
        WriteFile("|qwerty|User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithWhitespaceName_SkipsLine()
    {
        WriteFile("   |qwerty|User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEmptyPassword_SkipsLine()
    {
        WriteFile("alice||User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithWhitespacePassword_SkipsLine()
    {
        WriteFile("alice|   |User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUnknownRole_SkipsLine()
    {
        WriteFile("alice|qwerty|Superuser");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNumericRole_SkipsLine()
    {
        WriteFile("alice|qwerty|99");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithLowercaseRole_SkipsLine()
    {
        WriteFile("alice|qwerty|user");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Empty(users);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithExtraSpaces_ParsesCorrectly()
    {
        WriteFile("  alice  |  qwerty  |  User  ");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Single(users);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("qwerty", users[0].Password);
        Assert.Equal(Role.User, users[0].Role);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_DuplicateName_KeepsFirstSkipsSecond()
    {
        WriteFile("alice|qwerty|User", "alice|other|Admin");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Single(users);
        Assert.Equal("qwerty", users[0].Password);
        Assert.Equal(Role.User, users[0].Role);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_DuplicateNameDifferentCase_LoadsBoth()
    {
        WriteFile("alice|qwerty|User", "ALICE|other|Admin");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Equal(2, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("ALICE", users[1].Name);
        Assert.Equal(Role.User, users[0].Role);
        Assert.Equal(Role.Admin, users[1].Role);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_MixValidAndBroken_CountsSkippedCorrectly()
    {
        WriteFile(
            "alice|qwerty|User",
            "broken|line",
            "bob|secret|Admin",
            "|empty|name");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Equal(2, users.Count);
        Assert.Equal(2, skipped);
    }

    [Fact]
    public void LoadAll_MultipleLines_KeepsOrder()
    {
        WriteFile(
            "alice|qwerty|User",
            "bob|secret|Admin",
            "carol|pass|User");
        var repo = new UserRepository(_path);

        var users = repo.LoadAll(out var skipped);

        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("bob", users[1].Name);
        Assert.Equal("carol", users[2].Name);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void SaveAll_WritesAllUsersToFile()
    {
        var repo = new UserRepository(_path);
        var list = new List<User>
        {
            new User("alice", "qwerty", Role.User),
            new User("root", "secret", Role.Admin),
        };

        repo.SaveAll(list);

        var lines = ReadFile();
        Assert.Equal(2, lines.Length);
        Assert.Equal("alice|qwerty|User", lines[0]);
        Assert.Equal("root|secret|Admin", lines[1]);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_RoundTripPreservesData()
    {
        var repo = new UserRepository(_path);
        var original = new List<User>
        {
            new User("alice", "qwerty", Role.User),
            new User("root", "secret", Role.Admin),
        };

        repo.SaveAll(original);
        var loaded = repo.LoadAll(out var skipped);

        Assert.Equal(0, skipped);
        Assert.Equal(2, loaded.Count);
        Assert.Equal(original[0].Name, loaded[0].Name);
        Assert.Equal(original[0].Password, loaded[0].Password);
        Assert.Equal(original[0].Role, loaded[0].Role);
        Assert.Equal(original[1].Name, loaded[1].Name);
        Assert.Equal(original[1].Password, loaded[1].Password);
        Assert.Equal(original[1].Role, loaded[1].Role);
    }

    [Fact]
    public void SaveAll_EmptyList_WritesEmptyFile()
    {
        var repo = new UserRepository(_path);

        repo.SaveAll(new List<User>());

        Assert.True(File.Exists(_path));
        Assert.Empty(ReadFile());
    }

    [Fact]
    public void SaveAll_OverwritesPreviousContent()
    {
        WriteFile("old|pass|User");
        var repo = new UserRepository(_path);

        repo.SaveAll(new List<User>
        {
            new User("alice", "qwerty", Role.User),
        });

        var lines = ReadFile();
        Assert.Single(lines);
        Assert.Equal("alice|qwerty|User", lines[0]);
    }
}