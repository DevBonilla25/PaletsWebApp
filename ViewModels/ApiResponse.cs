namespace PaletsWebApp.ViewModels
{
    public sealed class ApiResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public object? Data { get; init; }
        public string? Code { get; init; }

        public static ApiResponse Succeeded(object? data, string message) => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        public static ApiResponse Failed(string message, string code) => new()
        {
            Success = false,
            Message = message,
            Data = null,
            Code = code
        };
    }
}
