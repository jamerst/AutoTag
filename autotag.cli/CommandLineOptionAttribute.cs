using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace autotag.cli;

[AttributeUsage(AttributeTargets.Property)]
public class CommandLineOptionAttribute<T> : Attribute
{
    public required string Name { get; init; }
    public string? ShortName { get; init; }
    public string? Description { get; init; }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name)
    {
        Name = name;
    }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string shortName) : this(name)
    {
        ShortName = shortName;
    }

    [SetsRequiredMembers]
    public CommandLineOptionAttribute(string name, string shortName, string description) : this(name, shortName)
    {
        Description = description;
    }

    public Option<T> GetOption()
    {
        Option<T> option = new(Name, Description);

        if (!string.IsNullOrEmpty(ShortName))
        {
            option.AddAlias(ShortName);
        }

        return option;
    }
}