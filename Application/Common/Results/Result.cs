namespace Application.Common.Results
{

    public class Result<T> : NoContentResult where T : class?
    {
        public T? Content { get; private set; }

        protected Result(
            bool isSuccess,
            T? content,
            IEnumerable<ErrorDto>? errors,
            ErrorType? errorType
        )
            : base(isSuccess, errors, errorType)
        {
            Content = content;
        }

        public static Result<T> CreateSuccessResult(T? content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));

            return new Result<T>(true, content, null, null);
        }

        public static Result<T> CreateErrorResult(
            IEnumerable<ErrorDto>? errors,
            ErrorType? errorType)
        {
            ArgumentNullException.ThrowIfNull(errors, nameof(errors));
            ArgumentNullException.ThrowIfNull(errorType, nameof(errorType));

            if (errors.Count() == 0)
            {
                throw Exceptions.EmptyErrorResultException.Create();
            }

            return new Result<T>(false, null, errors, errorType);
        }
    }
}
