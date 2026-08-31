using FluentValidation;

namespace Homevault.Application.Homes;

public sealed class CreateHomeValidator : AbstractValidator<CreateHomeCommand>
{
    public CreateHomeValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(200)
            .WithMessage("O nome da casa deve ter no máximo 200 caracteres.");
    }
}
