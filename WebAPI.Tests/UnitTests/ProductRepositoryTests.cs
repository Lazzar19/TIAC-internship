using System.Xml.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting.Internal;
using WebAPI.Infrastructure;

namespace WebAPI.Tests;

using WebAPI.Domain;
using WebAPI.Application;

using Xunit;
using FluentAssertions;


public class ProductRepositoryTests
{
    private static ApplicationDbContext CreateMockContext()
    {

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        
        return new ApplicationDbContext(options);
    }

    [Fact]

    public async Task GetAllyAsync_Returns_Correct_Pages()
    {
        await using var context = CreateMockContext();
        
        context.Products.AddRange(
            new Product {Name = "A", Description = "test1", Price = 15, Stock = 5},
            new Product { Name = "B", Description = "test2", Price = 25, Stock = 10 },
            new Product { Name = "C", Description = "test3", Price = 30, Stock = 10 },
            new Product { Name = "D", Description = "test4", Price = 20, Stock = 10 }
        );

        await context.SaveChangesAsync();
        var repository = new ProductRepository(context);
        var results = await repository.GetAllAsync(new ProductQuerryParametars { PageNumber = 1, PageSize = 2 });

        results.Items.Should().HaveCount(2);
        results.TotalCount.Should().Be(4);
        results.TotalPages.Should().Be(2);
    }


    [Fact]

    public async Task GetAllAsync_Filters_By_Price()
    {
        
        await using var context = CreateMockContext();

        context.Products.AddRange(
            new Product { Name = "Cheap", Description = "test1", Price = 15, Stock = 5 },
            new Product { Name = "Mid", Description = "test2", Price = 50, Stock = 5 },
            new Product { Name = "Expensive", Description = "test3", Price = 100, Stock = 5 }

        );

        await context.SaveChangesAsync();
        var repository = new ProductRepository(context);
        var results = await repository.GetAllAsync(new ProductQuerryParametars { MinPrice = 10, MaxPrice = 100 });
        results.Items.Should().ContainSingle(c => c.Name == "Mid");
        
        

    }


    [Fact]

    public async Task GetAllAsync_Filters_By_Search_Term()
    {
        await using var context = CreateMockContext();
        context.Products.AddRange(

            new Product { Name = "Laptop", Description = "test1", Price = 15, Stock = 5 },
            new Product { Name = "Desktop", Description = "test2", Price = 25, Stock = 10 }
        );
        
        await context.SaveChangesAsync();
        var repository = new ProductRepository(context);
        var results = await repository.GetAllAsync(new ProductQuerryParametars { Search = "Laptop" });
        results.Items.Should().ContainSingle(c => c.Name == "Laptop");
    }


    
}