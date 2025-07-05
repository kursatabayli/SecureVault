namespace SecureVault.Shared.Result
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }

        protected Result(bool isSuccess, Error error)
        {
            if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
            {
                throw new ArgumentException("Geçersiz Result durumu.", nameof(error));
            }
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);
    }

    public class Result<T> : Result
    {
        private readonly T? _value;

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Başarısız bir sonucun değeri okunamaz.");

        private Result(T value) : base(true, Error.None)
        {
            _value = value;
        }

        private Result(Error error) : base(false, error)
        {
            _value = default;
        }

        public static implicit operator Result<T>(T value) => new(value);

        public static implicit operator Result<T>(Error error) => new(error);
    }
}
