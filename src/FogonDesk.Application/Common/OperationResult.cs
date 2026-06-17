namespace FogonDesk.Application.Common
{
    public class OperationResult
    {
        public bool Success { get; protected set; }
        public string Message { get; protected set; }

        public static OperationResult Ok(string message = "")
        {
            return new OperationResult
            {
                Success = true,
                Message = message ?? string.Empty
            };
        }

        public static OperationResult Fail(string message)
        {
            return new OperationResult
            {
                Success = false,
                Message = message ?? string.Empty
            };
        }
    }

    public sealed class OperationResult<T> : OperationResult
    {
        public T Data { get; private set; }

        public static OperationResult<T> Ok(T data, string message = "")
        {
            return new OperationResult<T>
            {
                Success = true,
                Data = data,
                Message = message ?? string.Empty
            };
        }

        public new static OperationResult<T> Fail(string message)
        {
            return new OperationResult<T>
            {
                Success = false,
                Data = default(T),
                Message = message ?? string.Empty
            };
        }
    }
}
