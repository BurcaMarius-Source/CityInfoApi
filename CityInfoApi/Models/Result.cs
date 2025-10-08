namespace CityInfoApi.Models
{
    public sealed record Result<T>(bool Success, T? Data, string? Message, int StatusCode)
    {
        public static Result<T> Ok(T data, string? message = null) =>  new(true, data, message, StatusCodes.Status200OK);

        public static Result<T> Created(T data, string? message = null) =>  new(true, data, message, StatusCodes.Status201Created);

        public static Result<T> NoContent(string? message = null) => new(true, default, message, StatusCodes.Status204NoContent);

        public static Result<T> NotFound(string? message = null) => new(false, default, message, StatusCodes.Status404NotFound);

        public static Result<T> Conflict(string? message = null) => new(false, default, message, StatusCodes.Status409Conflict);

        public static Result<T> BadRequest(string? message = null) =>  new(false, default, message, StatusCodes.Status400BadRequest);
    }
}
