namespace Application.Common.Exceptions
{
    public class EmptyErrorResultException : Exception
    {
        private EmptyErrorResultException(string message) : base(message)
        {
        }

        public static EmptyErrorResultException Create()
        {
            return new EmptyErrorResultException("Error list for error result is empty.");
        }
    }
}
