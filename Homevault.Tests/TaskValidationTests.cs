using System.Text.Json;
using Homevault_api.Controllers;
using Xunit;

namespace Homevault.Tests;

public sealed class TaskValidationTests
{
    private readonly CreateTaskValidator createValidator = new();
    private readonly PatchTaskValidator patchValidator = new();

    [Theory]
    [InlineData("title", 120)]
    [InlineData("meta", 60)]
    [InlineData("assignee", 40)]
    public void Create_And_Patch_Enforce_Trimmed_Boundaries(string field, int maximum)
    {
        foreach (var length in new[] { 0, 1, maximum, maximum + 1 })
        {
            var text = $" {new string('x', length)} ";
            var request = new CreateTaskRequest(field == "title" ? text : "Titulo",
                field == "meta" ? text : null, field == "assignee" ? text : null);
            var patch = JsonSerializer.Deserialize<PatchTaskRequest>(
                JsonSerializer.Serialize(new Dictionary<string, string> { [field] = text }), JsonSerializerOptions.Web)!;
            var expected = length >= 1 && length <= maximum;
            Assert.Equal(expected, createValidator.Validate(request).IsValid);
            Assert.Equal(expected, patchValidator.Validate(patch).IsValid);
        }
    }

    [Fact]
    public void Create_Requires_Title_And_Defaults_Optional_Fields()
    {
        Assert.False(createValidator.Validate(new CreateTaskRequest(null)).IsValid);
        Assert.True(createValidator.Validate(new CreateTaskRequest("Titulo")).IsValid);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{\"meta\":null}")]
    [InlineData("{\"assignee\":null}")]
    [InlineData("{\"done\":null}")]
    public void Patch_Rejects_Empty_Or_Explicit_Null(string json)
    {
        var patch = JsonSerializer.Deserialize<PatchTaskRequest>(json, JsonSerializerOptions.Web)!;
        Assert.False(patchValidator.Validate(patch).IsValid);
    }

    [Fact]
    public void Patch_Detects_Presence_Including_False_And_Rejects_Unknown_Fields()
    {
        var patch = JsonSerializer.Deserialize<PatchTaskRequest>("{\"done\":false}", JsonSerializerOptions.Web)!;
        Assert.True(patchValidator.Validate(patch).IsValid);
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<PatchTaskRequest>("{\"unknown\":true}", JsonSerializerOptions.Web));
    }
}
