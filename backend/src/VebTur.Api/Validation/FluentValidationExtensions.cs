using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VebTur.Api.Validation;

/// <summary>
/// Small hand-rolled replacement for FluentValidation.AspNetCore's ToModelStateDictionary()
/// (that package is archived/deprecated) — just enough to feed a FluentValidation result into
/// ControllerBase.ValidationProblem(ModelStateDictionary).
/// </summary>
public static class FluentValidationExtensions
{
    public static ModelStateDictionary ToModelStateDictionary(this ValidationResult result)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return modelState;
    }
}
