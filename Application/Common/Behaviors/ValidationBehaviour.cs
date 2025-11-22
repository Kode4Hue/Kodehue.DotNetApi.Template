using FluentValidation;
using FluentValidation.Results;
using Mediator;
using System.Reflection;
using Application.Common.Results;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Application.Common.Behaviors
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, MessageHandlerDelegate<TRequest, TResponse> next)
        {
            if (_validators != null && _validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    var errors = failures.Select(f => ErrorDto.CreateFromMessage(f.ErrorMessage)).ToList();

                    var responseType = typeof(TResponse);
                    if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var method = responseType.GetMethod("CreateErrorResult", BindingFlags.Public | BindingFlags.Static);
                        if (method != null)
                        {
                            var created = method.Invoke(null, new object[] { errors, ErrorType.ValidationError });
                            return (TResponse)created!;
                        }
                    }

                    throw new ValidationException(failures);
                }
            }

            return await next(request, cancellationToken);
        }
    }
}
