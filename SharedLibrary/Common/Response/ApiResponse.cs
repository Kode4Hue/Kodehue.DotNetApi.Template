namespace SharedLibrary.Common.Response
{
    /// <summary>
    /// Common API response wrapper for consistent client handling.
    /// </summary>
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
