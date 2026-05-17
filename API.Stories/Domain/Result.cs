namespace API.Stories.Domain;

public record struct Result(
    in string Error = default)
{
    public static Result Ok => new();
    public readonly bool Success => Error is null;

    public static implicit operator Result(in string error) => new(error);
    public static implicit operator bool(Result r) => r.Success;
}

public record struct Result<T>(
    in T Data,
    in string Error = default)
{
    public readonly bool Success => Error is null;

    public static implicit operator Result<T>(in T data) => new(data);
    public static implicit operator Result<T>(in string error) => new(default, error);
    public static implicit operator Result(Result<T> result) => new(result.Error);
    public static implicit operator bool(Result<T> r) => r.Success;

    public static implicit operator T(Result<T> result) =>
        result.Success ?
            result.Data : throw new NotSupportedException();
}