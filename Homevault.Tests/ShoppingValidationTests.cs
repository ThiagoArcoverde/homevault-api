using Homevault_api.Controllers;
using Xunit;

namespace Homevault.Tests;

public sealed class ShoppingValidationTests
{
    private readonly CreateShoppingItemValidator createValidator = new();
    private readonly PatchShoppingItemValidator patchValidator = new();

    [Theory]
    [InlineData(null, 1)]
    [InlineData("  ", 1)]
    [InlineData("Nome", 0)]
    [InlineData("Nome", 1000)]
    public void Create_Rejects_Invalid_Name_Or_Quantity(string? name, int quantity)
    {
        Assert.False(createValidator.Validate(new CreateShoppingItemRequest(name, quantity, Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Create_Accepts_Trimmed_Name_Within_Limit()
    {
        Assert.True(createValidator.Validate(new CreateShoppingItemRequest("  Leite  ", 999, Guid.NewGuid())).IsValid);
        Assert.False(createValidator.Validate(new CreateShoppingItemRequest(new string('X', 81), 1, Guid.NewGuid())).IsValid);
        Assert.False(createValidator.Validate(new CreateShoppingItemRequest("Leite", 1, Guid.Empty)).IsValid);
    }

    [Fact]
    public void Patch_Validates_Only_Provided_Fields()
    {
        Assert.True(patchValidator.Validate(new PatchShoppingItemRequest { Purchased = true }).IsValid);
        Assert.False(patchValidator.Validate(new PatchShoppingItemRequest { Name = null }).IsValid);
        Assert.False(patchValidator.Validate(new PatchShoppingItemRequest { CategoryId = Guid.Empty }).IsValid);
        Assert.False(patchValidator.Validate(new PatchShoppingItemRequest { Quantity = null }).IsValid);
    }
}
