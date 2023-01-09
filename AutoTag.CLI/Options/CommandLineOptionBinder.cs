namespace AutoTag.CLI.Options;

public class CommandLineOptionBinder<T> : BinderBase<T>
    where T : IOptionsBase<T>
{
    protected override T GetBoundValue(BindingContext bindingContext)
    {
        return T.GetBoundValues(bindingContext);
    }
}