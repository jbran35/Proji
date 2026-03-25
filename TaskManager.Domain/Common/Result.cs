using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Common
{
    public class Result 
    {
        public bool IsSuccess { get; }
        public string? SuccessMessage { get; }
        public string? ErrorMessage { get; }
        public ErrorCode ErrorCode { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, string? successMessage, string? errorMessage, ErrorCode errorCode)
        {
            IsSuccess = isSuccess;
            SuccessMessage = successMessage;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
        
        public static Result Success(string successMessage = "") => new(true, successMessage, null, ErrorCode.None);
        public static Result Failure(ErrorCode errorCode, string errorMessage) => new(false, null, errorMessage, errorCode);
    }

    public class Result<T> : Result
    {
        private readonly T? _value;
        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failure result can't be accessed.");

        private Result(T? value, bool isSuccess, string success, string error, ErrorCode code)
            : base(isSuccess, success, error, code)
        {
            _value = value;
        }

        public static Result<T> Success(T value, string success="") => new(value, true, success, string.Empty, ErrorCode.None);
        public new static Result<T> Failure(ErrorCode code, string error) => new(default, false, string.Empty, error, code);

    }
}
