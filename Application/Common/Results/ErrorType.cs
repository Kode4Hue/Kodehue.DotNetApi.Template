namespace Application.Common.Results
{
    public sealed class ErrorType
    {
        public string Value { get; }

        private ErrorType(string value) => Value = value;

        public static readonly ErrorType ValidationError = new("Validation_Error");
        public static readonly ErrorType ApplicationError = new("Application_Error");
        public static readonly ErrorType NotFoundError = new("Not_Found_Error");
        public static readonly ErrorType Conflict = new("Conflict");

        public override string ToString() => Value;

        public override bool Equals(object? obj) =>
            obj is ErrorType other && Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(ErrorType left, ErrorType right) =>
            left.Equals(right);

        public static bool operator !=(ErrorType left, ErrorType right) =>
            !left.Equals(right);
    }
}
