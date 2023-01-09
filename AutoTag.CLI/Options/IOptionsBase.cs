namespace AutoTag.CLI.Options;

public interface IOptionsBase<TOptions>
{
    static abstract IEnumerable<Option> GetOptions();
    static abstract TOptions GetBoundValues(BindingContext context);
}