using DefneAI.Application.DTOs;
using FluentValidation;

namespace DefneAI.Application.Validators;

public sealed class AddModelDtoValidator : AbstractValidator<AddModelDto>
{
    public AddModelDtoValidator()
    {
        RuleFor(model => model.ModelName)
            .NotEmpty()
            .WithMessage("Model adı boş olamaz.");

        RuleFor(model => model.Provider)
            .NotEmpty()
            .WithMessage("Sağlayıcı adı boş olamaz.");

        RuleFor(model => model.ApiKey)
            .NotEmpty()
            .WithMessage("API key boş olamaz.");

        RuleFor(model => model.ModelPurpose)
            .NotEmpty()
            .WithMessage("Model amacı boş olamaz.");

        RuleFor(model => model.ModelDescription)
            .NotEmpty()
            .WithMessage("Model açıklaması boş olamaz.");

        RuleFor(model => model.Temperature)
            .InclusiveBetween(0, 2)
            .WithMessage("Temperature 0 ile 2 arasında olmalıdır.");

        RuleFor(model => model.PriorityNumber)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority negatif olamaz.");
    }
}
