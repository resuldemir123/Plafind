using FluentValidation;
using Plafind.Features.Businesses.ViewModels;

namespace Plafind.Features.Businesses.Validators
{
    public class CreateBusinessViewModelValidator : AbstractValidator<CreateBusinessViewModel>
    {
        public CreateBusinessViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("İşletme adı gereklidir")
                .MaximumLength(200).WithMessage("İşletme adı en fazla 200 karakter olabilir");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
                .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Geçerli bir telefon numarası formatı giriniz")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Website)
                .Must(BeValidUrl).WithMessage("Geçerli bir web sitesi URL'si giriniz")
                .When(x => !string.IsNullOrEmpty(x.Website));

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Enlem -90 ile 90 arasında olmalıdır")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Boylam -180 ile 180 arasında olmalıdır")
                .When(x => x.Longitude.HasValue);
        }

        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}

