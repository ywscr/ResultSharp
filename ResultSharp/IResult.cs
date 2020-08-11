namespace ResultSharp
{
    internal interface IResult
    {
        bool IsOk { get; }
        bool IsErr { get; }

        object UnwrapErrUntyped();
    }
}