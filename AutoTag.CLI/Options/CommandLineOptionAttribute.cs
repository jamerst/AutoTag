using System.Diagnostics.CodeAnalysis;

namespace AutoTag.CLI.Options;

[AttributeUsage(AttributeTargets.Property)]
public class CommandLineOptionAttribute<T> : Attribute
{
    public required string Name { get; init; }
    public string? ShortName { get; init; }
    public string? Description { get; init; }

    private bool DefaultValueProvided { get; set; }
    public T? DefaultValue { get; set; }

    public bool UseInitialValueAsDefault { get; set; }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string shortName, T defaultValue) : this(name, shortName)
    {
        DefaultValueProvided = true;
        DefaultValue = defaultValue;
    }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string shortName, string description) : this(name, description)
    {
        ShortName = shortName;
    }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string shortName, string description, T defaultValue) : this(name, shortName, description)
    {
        DefaultValueProvided = true;
        DefaultValue = defaultValue;
    }

    public Option<T> GetOption()
    {
        Option<T> option = new(Name, Description);

        if (!string.IsNullOrEmpty(ShortName))
        {
            option.AddAlias(ShortName);
        }

        if (DefaultValueProvided)
        {
            option.SetDefaultValue(DefaultValue);
        }

        return option;
    }
}