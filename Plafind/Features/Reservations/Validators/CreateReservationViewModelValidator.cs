using FluentValidation;
using Plafind.Features.Reservations.ViewModels;

namespace Plafind.Features.Reservations.Validators
{
    public class CreateReservationViewModelValidator : AbstractValidator<CreateReservationViewModel>
    {
        public CreateReservationViewModelValidator()
        {
            RuleFor(x => x.BusinessId)
                .GreaterThan(0).WithMessage("Geçerli bir işletme seçiniz");

            RuleFor(x => x.RequestedDate)
                .NotEmpty().WithMessage("Rezervasyon tarihi gereklidir")
                .Must(BeFutureDate).WithMessage("Rezervasyon tarihi bugünden ileri bir tarih olmalıdır");

            RuleFor(x => x.RequestedTime)
                .NotEmpty().WithMessage("Rezervasyon saati gereklidir");

            RuleFor(x => x.NumberOfPeople)
                .GreaterThan(0).WithMessage("Kişi sayısı 1'den büyük olmalıdır")
                .LessThanOrEqualTo(50).WithMessage("Kişi sayısı 50'den fazla olamaz");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notlar en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Notes));

            RuleFor(x => x.ContactPhone)
                .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
                .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Geçerli bir telefon numarası formatı giriniz")
                .When(x => !string.IsNullOrEmpty(x.ContactPhone));

            RuleFor(x => x.ContactEmail)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
                .When(x => !string.IsNullOrEmpty(x.ContactEmail));
        }

        private bool BeFutureDate(DateTime date)
        {
            return date.Date >= DateTime.Today;
        }
    }
}

