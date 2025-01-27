using System.Diagnostics.CodeAnalysis;

namespace EntityArchitect.CRUD.Results.Abstracts;

public class Result
{
    protected internal Result()
    {
        IsSuccess = true;
    }

    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error is not null) throw new InvalidOperationException();

        if (!isSuccess && error is null) throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Errors.Add(error);
    }

    protected internal Result(bool isSuccess, List<Error> errors)
    {
        if (isSuccess && errors.Count > 0) throw new InvalidOperationException();

        if (!isSuccess && errors.Count == 0) throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public List<Error> Errors { get; } = new();

    public static Result Success()
    {
        return new Result();
    }

    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }

    public static Result Failure(List<Error> errors)
    {
        return new Result(false, errors);
    }

    public static Result<TValue?> Success<TValue>(TValue? value) where TValue : class
    {
        return new Result<TValue?>(value);
    }

    public static Result<TValue> Failure<TValue>(Error error) where TValue : class
    {
        return new Result<TValue>(default, false, error);
    }

    public static Result<TValue> Failure<TValue>(List<Error> errors) where TValue : class
    {
        return new Result<TValue>(default, false, errors);
    }

    public static Result<TValue?> Create<TValue>(TValue? value) where TValue : class
    {
        return value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
    }
}

public class Result<TValue> : Result where TValue : class
{
    private readonly TValue? _value;

    protected internal Result(TValue? value)
    {
        _value = value;
    }

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    protected internal Result(TValue? value, bool isSuccess, List<Error> errors) : base(isSuccess, errors)
    {
        _value = value;
    }

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : null;

    public static implicit operator Result<TValue?>(TValue? value)
    {
        return Create(value);
    }
}

public class ResultModel
{
    public bool IsSuccess { get; set; }
    public List<Error> Errors { get; set; }
}