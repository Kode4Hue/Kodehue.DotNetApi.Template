using Application.Account.Users.Requests.SignupAttendee;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests
{
    public class SignupValidationTests
    {
        [Fact]
        public void Validator_Fails_WhenEmailAndPasswordInvalid()
        {
            var validator = new SignupAttendeeCommandValidator();
            var command = new SignupAttendeeCommand { Email = "", Password = "" };

            var result = validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.Email);
            result.ShouldHaveValidationErrorFor(c => c.Password);
        }

        [Fact]
        public void Validator_Passes_WhenValid()
        {
            var validator = new SignupAttendeeCommandValidator();
            var command = new SignupAttendeeCommand { Email = "user@example.com", Password = "secret" };

            var result = validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Email);
            result.ShouldNotHaveValidationErrorFor(c => c.Password);
        }
    }
}
