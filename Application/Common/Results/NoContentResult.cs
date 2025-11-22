using Application.Common.Exceptions;

namespace Application.Common.Results
{
    public class NoContentResult
    {

        public bool IsSuccess { get; private set; }
        public IEnumerable<ErrorDto>? Errors { get; private set; }
        public ErrorType? ErrorType { get; set; }

        protected NoContentResult(
            bool isSuccess,
            IEnumerable<ErrorDto>? errors = null,
            ErrorType? errorType = default)
        {
            IsSuccess = isSuccess;
            Errors = errors;
            ErrorType = errorType;
        }

        public static NoContentResult CreateSuccess()
        {
            return new NoContentResult(true, null);
        }

        public static NoContentResult CreateErrorResult(
            List<ErrorDto>? errors,
            ErrorType? errorType)
        {
            ArgumentNullException.ThrowIfNull(errors, nameof(errors));
            ArgumentNullException.ThrowIfNull(errorType, nameof(errorType));

            if (errors.Count == 0)
            {
                throw EmptyErrorResultException.Create();
            }

            return new NoContentResult(false, errors, errorType);
        }
    }
}
