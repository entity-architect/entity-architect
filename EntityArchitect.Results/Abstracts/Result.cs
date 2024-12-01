using System.Diagnostics.CodeAnalysis;

namespace EntityArchitect.Results.Abstracts;

public class Result
{
    protected internal Result()
    {
        IsSuccess = true;
    }

    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        _errors.Add(error);
    }

    protected internal Result(bool isSuccess, List<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        _errors = errors;
    }

    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;

    private List<Error> _errors = new();
    public List<Error> Errors => _errors;

    public static Result Success() => new();
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(List<Error> errors) => new(false, errors);
    public static Result<TValue> Success<TValue>(TValue value) => new(value);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
    public static Result<TValue> Failure<TValue>(List<Error> errors) => new(default, false, errors);

    public static Result<TValue> Create<TValue>(TValue? value) => value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value) : base()
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
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue? value) => Create(value);
}
    
public class ResultModel
{
    public bool IsSuccess { get; set; }
    public List<Error> Errors { get; set; }
}