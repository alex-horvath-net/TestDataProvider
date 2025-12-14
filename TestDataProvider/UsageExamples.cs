using System;
using System.Collections.Immutable;
using TestDataProviderCore;

public static class UsageExamples
{
    public static void Run()
    {
        var provider = new Provider();
        var user = provider.Create<User>();
        Console.WriteLine($"User: {user.Name}, {user.Age}");

        var orders = provider.CreateMany<Order>(2);
        foreach (var o in orders) Console.WriteLine($"Order {o.Id}: {o.Product}");

        var imList = provider.Create<ImmutableList<string>>();
        Console.WriteLine($"ImmutableList size: {imList.Count}");
    }
}

public record User(string Name, int Age);
public class Order { public Order(int id, string product) { Id = id; Product = product; } public int Id { get; } public string Product { get; } }
