using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Primitives;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, string error,ErrorType errorType)
    {
       
        if (isSuccess && error != string.Empty)
            throw new InvalidOperationException("Un resultado exitoso no puede tener mensaje de error.");

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Un resultado fallido debe tener un mensaje de error.");

        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Fail(string message,ErrorType errorType=ErrorType.Validation) => new Result(false, message,errorType);
    public static Result Success() => new Result(true, string.Empty,ErrorType.None);

   
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Fail<T>(string message, ErrorType errorType = ErrorType.Validation)
        => Result<T>.Fail(message, errorType);
}

public class Result<T> : Result
{
    private readonly T? _value;


    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    protected Result(T? value, bool isSuccess, string error, ErrorType errorType)
         : base(isSuccess, error, errorType)
    {
        _value = value;
    }

 
    public static Result<T> Success(T value)
        => new Result<T>(value, true, string.Empty, ErrorType.None);

   
    public new static Result<T> Fail(string message, ErrorType errorType = ErrorType.Validation)
        => new Result<T>(default, false, message, errorType);
}