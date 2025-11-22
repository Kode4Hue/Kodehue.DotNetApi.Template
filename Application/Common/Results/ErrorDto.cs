namespace Application.Common.Results
{
    public class ErrorDto
    {
        public string Message { get; private set; }
        public Dictionary<string, string>? Metadata { get; private set; }

        private ErrorDto(
            string message,
            Dictionary<string, string>? metadata)
        {
            Message = message;
            Metadata = metadata;
        }

        public static ErrorDto CreateFromMessage(
            string message,
            Dictionary<string, string>? metadata = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            return new ErrorDto(
                message,
                metadata);
        }
    }
}
