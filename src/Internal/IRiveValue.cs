namespace VL.Rive.Internal;

internal interface IRiveValue
{
    object? Value { get; set; }
}

internal interface IRiveValue<T> : IRiveValue
{
    T? TypedValue { get; set; }
}
