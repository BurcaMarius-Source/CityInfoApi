using CityInfoApi.Models;
using FluentValidation;

namespace CityInfoApi.Validators
{
    public class AddCityRequestValidator : AbstractValidator<AddCityRequest>
    {
        public AddCityRequestValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
            RuleFor(c => c.State).MaximumLength(100);
            RuleFor(c => c.Country).NotEmpty().MaximumLength(100);
            RuleFor(c => c.TouristRating).InclusiveBetween(1, 5);
            RuleFor(c => c.EstimatedPopulation).GreaterThanOrEqualTo(0);
            RuleFor(c => c.DateEstablished).LessThanOrEqualTo(DateTime.Today);
        }
    }

    public class UpdateCityRequestValidator : AbstractValidator<UpdateCityRequest>
    {
        public UpdateCityRequestValidator()
        {
            RuleFor(c => c.TouristRating).InclusiveBetween(1, 5);
            RuleFor(c => c.EstimatedPopulation).GreaterThanOrEqualTo(0);
            RuleFor(c => c.DateEstablished).LessThanOrEqualTo(DateTime.Today);
        }
    }
}
