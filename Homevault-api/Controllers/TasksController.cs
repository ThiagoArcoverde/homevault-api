using System.Text.Json.Serialization;
using Asp.Versioning;
using FluentValidation;
using Homevault.Application.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Homevault_api.Controllers;

public sealed record CreateTaskRequest(string? Title, string? Meta = null, string? Assignee = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PatchTaskRequest
{
    private string? title;
    private string? meta;
    private string? assignee;
    private bool? done;

    public string? Title { get => title; set { title = value; HasTitle = true; } }
    public string? Meta { get => meta; set { meta = value; HasMeta = true; } }
    public string? Assignee { get => assignee; set { assignee = value; HasAssignee = true; } }
    public bool? Done { get => done; set { done = value; HasDone = true; } }

    internal bool HasTitle { get; private set; }
    internal bool HasMeta { get; private set; }
    internal bool HasAssignee { get; private set; }
    internal bool HasDone { get; private set; }
    internal bool HasAnyField => HasTitle || HasMeta || HasAssignee || HasDone;
}

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskValidator()
    {
        RuleFor(request => request.Title).Must(value => value is not null && value.Trim().Length is >= 1 and <= 120);
        RuleFor(request => request.Meta).Must(value => value is null || value.Trim().Length is >= 1 and <= 60);
        RuleFor(request => request.Assignee).Must(value => value is null || value.Trim().Length is >= 1 and <= 40);
    }
}

public sealed class PatchTaskValidator : AbstractValidator<PatchTaskRequest>
{
    public PatchTaskValidator()
    {
        RuleFor(request => request.HasAnyField).Equal(true).WithMessage("O patch não pode estar vazio.");
        RuleFor(request => request.Title).Must(value => value is not null && value.Trim().Length is >= 1 and <= 120)
            .When(request => request.HasTitle);
        RuleFor(request => request.Meta).Must(value => value is not null && value.Trim().Length is >= 1 and <= 60)
            .When(request => request.HasMeta);
        RuleFor(request => request.Assignee).Must(value => value is not null && value.Trim().Length is >= 1 and <= 40)
            .When(request => request.HasAssignee);
        RuleFor(request => request.Done).Must(value => value.HasValue).When(request => request.HasDone);
    }
}

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tasks")]
public sealed class TasksController(HomeTasks tasks, IValidator<CreateTaskRequest> createValidator,
    IValidator<PatchTaskRequest> patchValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(TasksResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken token) => Ok(await tasks.ListAsync(token));

    [HttpPost]
    [ProducesResponseType(typeof(TaskView), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken token)
    {
        var validation = await createValidator.ValidateAsync(request, token);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var task = await tasks.CreateAsync(request.Title!, request.Meta, request.Assignee, token);
        return Created($"/api/v1/tasks/{task.Id}", task);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(TaskView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Patch(string id, [FromBody] PatchTaskRequest request, CancellationToken token)
    {
        if (!Guid.TryParse(id, out var taskId)) return Problem(statusCode: 400, title: "ID inválido.");
        var validation = await patchValidator.ValidateAsync(request, token);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var task = await tasks.UpdateAsync(taskId, new TaskPatch(request.Title, request.Meta, request.Assignee, request.Done), token);
        return task is null ? Problem(statusCode: 404, title: "Afazer não encontrado.") : Ok(task);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(string id, CancellationToken token)
    {
        if (!Guid.TryParse(id, out var taskId)) return Problem(statusCode: 400, title: "ID inválido.");
        return await tasks.DeleteAsync(taskId, token) ? NoContent() : Problem(statusCode: 404, title: "Afazer não encontrado.");
    }
}
