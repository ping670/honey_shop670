using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace pcheli_diplomenproekt.Services;

/// <summary>
/// Задава български съобщения по подразбиране на валидационните атрибути, за които не е посочено ErrorMessage
/// (иначе ASP.NET Core връща английските съобщения на DataAnnotations).
/// </summary>
public class BulgarianValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attr in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (attr.ErrorMessage != null || attr.ErrorMessageResourceName != null) continue;

            attr.ErrorMessage = attr switch
            {
                RequiredAttribute => "Полето е задължително.",
                MaxLengthAttribute => "Текстът е твърде дълъг (най-много {1} символа).",
                MinLengthAttribute => "Изисква се поне {1} елемента/символа.",
                StringLengthAttribute => "Дължината трябва да е между {2} и {1} символа.",
                RangeAttribute => "Стойността трябва да е между {1} и {2}.",
                EmailAddressAttribute => "Невалиден имейл адрес.",
                PhoneAttribute => "Невалиден телефонен номер.",
                _ => "Невалидна стойност."
            };
        }
    }
}
