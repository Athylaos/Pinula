using FluentAssertions;
using Microsoft.Data.Sqlite;
using Pinula.API.Context;
using Pinula.API.Models;
using Pinula.API.Services;
using Pinula.API.Tests.TestHelpers;

namespace Pinula.API.Tests.Services;

public class CategoryBackendServiceTests : IDisposable
{
    private readonly PinulaDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly CategoryBackendService _service;

    public CategoryBackendServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _service = new CategoryBackendService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetCategoriesTree_ShouldBuildNestedTreeAndLocalizeNamesCorrectly()
    {
        var parentCatId = Guid.NewGuid();
        var childCatId = Guid.NewGuid();

        var parentCategory = new Category
        {
            Id = parentCatId,
            ParentCategoryId = null,
            SortOrder = 1,
            PictureUrl = "main.png",
            Names = new Dictionary<string, string>
            {
                { "cs", "Chody" },
                { "en", "Courses" }
            }
        };

        var childCategory = new Category
        {
            Id = childCatId,
            ParentCategoryId = parentCatId,
            SortOrder = 1,
            PictureUrl = "desserts.png",
            Names = new Dictionary<string, string>
            {
                { "cs", "Dezerty" },
                { "en", "Desserts" }
            }
        };

        _db.Categories.AddRange(parentCategory, childCategory);
        await _db.SaveChangesAsync();

        const string baseUrl = "https://pinula.app/images/categories/";
        const string lang = "cs";

        var result = await _service.GetCategoriesTree(baseUrl, lang);

        result.Should().NotBeNull();
        result.Should().HaveCount(1, "only one category is main");

        var root = result.First();
        root.Id.Should().Be(parentCatId);
        root.Name.Should().Be("Chody", "with lang=cs should be czech name");
        root.PictureUrl.Should().Be("https://pinula.app/images/categories/main.png");

        root.ChildCategories.Should().HaveCount(1, "root category should have 1 child category");
        var child = root.ChildCategories.First();
        child.Id.Should().Be(childCatId);
        child.Name.Should().Be("Dezerty");
        child.PictureUrl.Should().Be("https://pinula.app/images/categories/desserts.png");
    }

    [Theory]
    [InlineData("es")]
    [InlineData("cs")]
    public async Task GetCategoriesTree_ShouldFallbackToEnglish_WhenRequestedLanguageIsNotAvailableOrEmpty(
        string requestedLang)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            SortOrder = 1,
            PictureUrl = "",
            Names = new Dictionary<string, string>
            {
                { "cs", "" },
                { "en", "Breakfast" }
            }
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = await _service.GetCategoriesTree("https://pinula.app/", requestedLang);


        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Breakfast",
            "language should fallback to english if demanded language is not available or empty");
        result.First().PictureUrl.Should()
            .Contain("default_category.png", "with null/empty picture should fallback to default");
    }
}