using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Models;
using Pinula.API.Services;
using Pinula.API.Tests.TestHelpers;
using Moq;
using Pinula.Shared.DTOs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Pinula.API.Tests.Services;

public class CategoryBackendServiceTests : IDisposable
{
    private readonly PinulaDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly CategoryBackendService _service;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly string _tempFolderPath;

    public CategoryBackendServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _service = new CategoryBackendService(_db);
        
        _tempFolderPath = Path.Combine(Path.GetTempPath(), "PinulaTestUploads_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempFolderPath);

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_tempFolderPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempFolderPath))
        {
            Directory.Delete(_tempFolderPath, recursive: true);
        }
        
        _connection.Close();
        _connection.Dispose();
    }

    #region GetCategoriesTree
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
    #endregion

    #region GetCategoryWithChildren
    [Fact]
    public async Task GetCategoryWithChildren_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        var result = await _service.GetCategoryWithChildren("https://pinula.app/", "cs", Guid.NewGuid());
        
        result.Should().BeNull("Should return null without existing categoryId");
    }
    
    [Fact]
    public async Task GetCategoryWithChildren_ShouldReturnCategoryWithSortedChildrenAndCorrectLocalization()
    {
        var parentId = Guid.NewGuid();

        var parentCategory = new Category
        {
            Id = parentId,
            ParentCategoryId = null,
            SortOrder = 1,
            PictureUrl = "parent.png",
            Names = new Dictionary<string, string>
            {
                { "cs", "Hlavní jídla" },
                { "en", "Main Courses" }
            }
        };
        
        var child2 = new Category
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = parentId,
            SortOrder = 2,
            PictureUrl = "child2.png",
            Names = new Dictionary<string, string> { { "cs", "Omáčky" }, { "en", "Sauces" } }
        };

        var child1 = new Category
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = parentId,
            SortOrder = 1,
            PictureUrl = "child1.png",
            Names = new Dictionary<string, string> { { "cs", "Polévky" }, { "en", "Soups" } }
        };

        _db.Categories.AddRange(parentCategory, child2, child1);
        await _db.SaveChangesAsync();

        const string baseUrl = "https://pinula.app/images/categories/";
        
        var result = await _service.GetCategoryWithChildren(baseUrl, "en", parentId);
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(parentId);
        result.Name.Should().Be("Main Courses");
        result.PictureUrl.Should().Be("https://pinula.app/images/categories/parent.png");
        
        result.ChildCategories.Should().HaveCount(2);
        result.ChildCategories.ElementAt(0).Name.Should().Be("Soups", "Category with lowest sort order should be first");
        result.ChildCategories.ElementAt(1).Name.Should().Be("Sauces");
        result.ChildCategories.ElementAt(0).ChildCategories.Should().BeEmpty("Subcategories doesn't load theirs subcategories");
    }
    
    [Theory]
    [InlineData("es")]
    [InlineData("cs")]
    public async Task GetCategoryWithChildren_ShouldFallbackToEnglish_WhenRequestedLanguageIsNotAvailable(string requestedLang)
    {
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            SortOrder = 1,
            PictureUrl = "",
            Names = new Dictionary<string, string>
            {
                { "en", "Desserts" }
            }
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        
        var result = await _service.GetCategoryWithChildren("https://pinula.app/images/categories/", requestedLang, categoryId);
        
        result.Should().NotBeNull();
        result.Name.Should().Be("Desserts",
            "language should fallback to english if demanded language is not available or empty");
        result.PictureUrl.Should()
            .Contain("default_category.png", "with null/empty picture should fallback to default");
    }
    #endregion

    #region CreateCategory
    [Fact]
    public async Task CreateCategory_ShouldCreateCategoryWithDefaultImage_WhenImageFileIsNull()
    {
        var createDto = new CategoryCreateDto
        {
            Names = new Dictionary<string, string> { { "cs", "Káva" }, { "en", "Coffee" } },
            SortOrder = 5,
            ParentCategory = null
        };
        
        var result = await _service.CreateCategory(createDto, imageFile: null, _envMock.Object);
        
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Category created");
        result.CreatedCategoryId.Should().NotBeNull();

        var savedCategory = await _db.Categories.FindAsync(result.CreatedCategoryId);
        savedCategory.Should().NotBeNull();
        savedCategory!.Names["en"].Should().Be("Coffee");
        savedCategory.SortOrder.Should().Be(5);
        savedCategory.PictureUrl.Should().Be("default_category.png", "Should use default image, when non is provided");
    }
    
    [Theory]
    [InlineData("script.exe")]
    [InlineData("document.pdf")]
    [InlineData("archive.zip")]
    public async Task CreateCategory_ShouldReturn400_WhenImageExtensionIsUnsupported(string fileName)
    {
        var createDto = new CategoryCreateDto { Names = new Dictionary<string, string> { { "en", "Test" } }, SortOrder = 1};
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1024);
        
        var result = await _service.CreateCategory(createDto, fileMock.Object, _envMock.Object);
        
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Unsupported file format");
        
        var categoryCount = await _db.Categories.CountAsync();
        categoryCount.Should().Be(0, "Category shouldn't be saved when wrong image type is provided");
    }
    
    [Fact]
    public async Task CreateCategory_ShouldReturn500_WhenImageStreamIsCorrupted()
    {
        var createDto = new CategoryCreateDto { Names = new Dictionary<string, string> { { "en", "Test" } } };
        
        var invalidStream = new MemoryStream(Encoding.UTF8.GetBytes("This is not image"));

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.Length).Returns(invalidStream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(invalidStream);
        
        var result = await _service.CreateCategory(createDto, fileMock.Object, _envMock.Object);
        
        result.StatusCode.Should().Be(500);
        result.Message.Should().Be("Error while processing image");

        var categoryCount = await _db.Categories.CountAsync();
        categoryCount.Should().Be(0, "Category shouldn't be saved when processing image fails");
    }
    
    [Fact]
    public async Task CreateCategory_ShouldProcessImageAndSaveCategory_WhenValidImageIsProvided()
    {
        var createDto = new CategoryCreateDto
        {
            Names = new Dictionary<string, string> { { "cs", "Dezerty" }, { "en", "Desserts" } },
            SortOrder = 2
        };
        
        using var imageStream = new MemoryStream();
        using (var image = new Image<Rgba32>(10, 10))
        {
            await image.SaveAsPngAsync(imageStream);
        }
        imageStream.Position = 0;

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("dessert.png");
        fileMock.Setup(f => f.Length).Returns(imageStream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(imageStream);
        
        var result = await _service.CreateCategory(createDto, fileMock.Object, _envMock.Object);
        
        result.StatusCode.Should().Be(201);
        result.CreatedCategoryId.Should().NotBeNull();

        var savedCategory = await _db.Categories.FindAsync(result.CreatedCategoryId);
        savedCategory.Should().NotBeNull();
        savedCategory!.PictureUrl.Should().EndWith(".webp", "Image should be converted to .webp");
        savedCategory.PictureUrl.Should().NotBe("default_category.png");
        
        var expectedFilePath = Path.Combine(_tempFolderPath, "images", "categories", savedCategory.PictureUrl);
        File.Exists(expectedFilePath).Should().BeTrue("Image file should be on drive");
    }

    #endregion
    

}