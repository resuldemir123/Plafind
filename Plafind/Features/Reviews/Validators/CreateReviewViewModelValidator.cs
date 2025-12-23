using FluentValidation;
using Plafind.Features.Reviews.ViewModels;

namespace Plafind.Features.Reviews.Validators
{
    public class CreateReviewViewModelValidator : AbstractValidator<CreateReviewViewModel>
    {
        public CreateReviewViewModelValidator()
        {
            RuleFor(x => x.BusinessId)
                .GreaterThan(0).WithMessage("Geçerli bir işletme seçiniz");

            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Yorum metni gereklidir")
                .MaximumLength(1000).WithMessage("Yorum en fazla 1000 karakter olabilir");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Puan 1 ile 5 arasında olmalıdır");
        }
    }
}

