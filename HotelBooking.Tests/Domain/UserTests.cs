using System;
using Xunit;
using HotelBooking.Domain;

namespace HotelBooking.Tests;

public class UserTests
{
    [Fact]
    public void CreateValidUser_Ok()
    {
        var user = new User("alice", "qwerty", Role.User);

        Assert.Equal("alice", user.Name);
        Assert.Equal("qwerty", user.Password);
        Assert.Equal(Role.User, user.Role);
    }

    [Fact]
    public void CreateUserWithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new User("", "qwerty", Role.User));
    }

    [Fact]
    public void CreateUserWithEmptyPassword_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new User("alice", "", Role.User));
    }
}