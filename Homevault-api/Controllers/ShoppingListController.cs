using System.Text.Json.Serialization;
using Asp.Versioning;
using FluentValidation;
using Homevault.Application.Shopping;
using Microsoft.AspNetCore.Mvc;

namespace Homevault_api.Controllers;

public sealed record CreateShoppingItemRequest(string? Name, int Quantity, Guid CategoryId);
// A property setter records whether the field was present in the JSON patch.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PatchShoppingItemRequest
{
    private string? name;
    private int? quantity;
    private Guid? categoryId;
    private bool? purchased;

    public string? Name { get => name; set { name = value; HasName = true; } }
    public int? Quantity { get => quantity; set { quantity = value; HasQuantity = true; } }
    public Guid? CategoryId { get => categoryId; set { categoryId = value; HasCategoryId = true; } }
    public bool? Purchased { get => purchased; set { purchased = value; HasPurchased = true; } }

    [JsonIgnore] public bool HasName { get; private set; }
    [JsonIgnore] public bool HasQuantity { get; private set; }
    [JsonIgnore] public bool HasCategoryId { get; private set; }
    [JsonIgnore] public bool HasPurchased { get; private set; }
    [JsonIgnore] public bool HasAnyField => HasName || HasQuantity || HasCategoryId || HasPurchased;
}
public sealed record ShoppingCategoriesResponse(IReadOnlyList<ShoppingCategoryView> Categories);

public sealed class CreateShoppingItemValidator : AbstractValidator<CreateShoppingItemRequest>
{
    public CreateShoppingItemValidator()
    {
        RuleFor(request => request.Name).Must(name => name is not null && name.Trim().Length is >= 1 and <= 80);
        RuleFor(request => request.Quantity).InclusiveBetween(1, 999);
        RuleFor(request => request.CategoryId).NotEmpty();
    }
}

public sealed class PatchShoppingItemValidator : AbstractValidator<PatchShoppingItemRequest>
{
    public PatchShoppingItemValidator()
    {
        RuleFor(request => request.Name).Must(name => name is not null && name.Trim().Length is >= 1 and <= 80)
            .When(request => request.HasName);
        RuleFor(request => request.Quantity).Must(quantity => quantity is >= 1 and <= 999)
            .When(request => request.HasQuantity);
        RuleFor(request => request.CategoryId).Must(id => id.HasValue && id.Value != Guid.Empty)
            .When(request => request.HasCategoryId);
        RuleFor(request => request.Purchased).Must(purchased => purchased.HasValue)
            .When(request => request.HasPurchased);
    }
}

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/shopping-list")]
public sealed class ShoppingListController(ShoppingList shoppingList, IValidator<CreateShoppingItemRequest> createValidator,
    IValidator<PatchShoppingItemRequest> patchValidator) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ShoppingCategoriesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Categories(CancellationToken token) =>
        Ok(new ShoppingCategoriesResponse(await shoppingList.CategoriesAsync(token)));

    [HttpGet("items")]
    [ProducesResponseType(typeof(ShoppingPage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Items([FromQuery] string? search, [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 4, CancellationToken token = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || categoryId == Guid.Empty)
            return Problem(statusCode: 400, title: "Parâmetros de consulta inválidos.");
        return Ok(await shoppingList.ListAsync(search, categoryId, page, pageSize, token));
    }

    [HttpGet("items/{id}")]
    [ProducesResponseType(typeof(ShoppingItemView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Item(string id, CancellationToken token)
    {
        if (!Guid.TryParse(id, out var itemId)) return Problem(statusCode: 400, title: "ID inválido.");
        var result = await shoppingList.FindAsync(itemId, token);
        return result is null ? Problem(statusCode: 404, title: "Item não encontrado.") : Ok(result);
    }

    [HttpGet("export-data")]
    [ProducesResponseType(typeof(ShoppingSnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(CancellationToken token) => Ok(await shoppingList.ExportAsync(token));

    [HttpPost("items")]
    [ProducesResponseType(typeof(ShoppingItemView), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create(CreateShoppingItemRequest request, CancellationToken token)
    {
        var validation = await createValidator.ValidateAsync(request, token);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var item = await shoppingList.CreateAsync(request.Name!, request.Quantity, request.CategoryId, token);
        return Created($"/api/v1/shopping-list/items/{item.Id}", item);
    }

    [HttpPatch("items/{id}")]
    [ProducesResponseType(typeof(ShoppingItemView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Patch(string id, [FromBody] PatchShoppingItemRequest request, CancellationToken token)
    {
        if (!Guid.TryParse(id, out var itemId)) return Problem(statusCode: 400, title: "ID inválido.");
        if (!request.HasAnyField)
            return Problem(statusCode: 400, title: "O patch não pode estar vazio.");
        var validation = await patchValidator.ValidateAsync(request, token);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var item = await shoppingList.UpdateAsync(itemId,
            new ShoppingPatch(request.Name, request.Quantity, request.CategoryId, request.Purchased), token);
        return item is null ? Problem(statusCode: 404, title: "Item não encontrado.") : Ok(item);
    }

    [HttpDelete("items/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(string id, CancellationToken token)
    {
        if (!Guid.TryParse(id, out var itemId)) return Problem(statusCode: 400, title: "ID inválido.");
        return await shoppingList.DeleteAsync(itemId, token) ? NoContent() : Problem(statusCode: 404, title: "Item não encontrado.");
    }

    [HttpDelete("items")]
    [ProducesResponseType(typeof(ClearPurchasedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Clear([FromQuery] string? purchased, CancellationToken token)
    {
        if (Request.Query["purchased"].Count != 1 || purchased != "true")
            return Problem(statusCode: 400, title: "Informe purchased=true.");
        return Ok(await shoppingList.ClearAsync(token));
    }
}
