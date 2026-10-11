namespace medick_Terrible_Tooltips;

// Caller-side example, not a native argument compatibility assertion.
// Array slots are copied; referenced native objects are NOT deep-cloned.
public sealed class ArgumentSnapshot
{
    private readonly object[] arguments;
    public ArgumentSnapshot(object[] args) => arguments = (object[])args.Clone();
    public object[] CopyArguments() => (object[])arguments.Clone();
}
