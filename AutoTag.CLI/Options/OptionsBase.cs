using System.Linq.Expressions;
using System.Reflection;

namespace AutoTag.CLI.Options;

public abstract class OptionsBase<TOptions>
{
    private static Dictionary<string, Option> _propertyOptions = new Dictionary<string, Option>();
    protected static Option<TValue> GetOption<TValue>(Expression<Func<TOptions, TValue>> property, Action<Option<TValue>>? configure = null)
    {
        var info = GetPropertyInfo(property);
        var attr = info.GetCustomAttribute(typeof(CommandLineOptionAttribute<TValue>)) as CommandLineOptionAttribute<TValue>;
        if (attr != null)
        {
            var option = attr.GetOption();
            _propertyOptions[info.Name] = option;

            configure?.Invoke(option);

            return option;
        }
        else
        {
            throw new ArgumentException($"Invalid property: must have a {nameof(CommandLineOptionAttribute<TValue>)} attribute");
        }
    }

    protected static Option<TValue> GetOption<TValue>(Expression<Func<TOptions, TValue>> property, TValue defaultValue)
    {
        var option = GetOption(property);

        option.SetDefaultValue(defaultValue);

        return option;
    }

    protected static TValue GetValueForProperty<TValue>(Expression<Func<TOptions, TValue>> property, BindingContext context)
    {
        var info = GetPropertyInfo(property);

        if (_propertyOptions.TryGetValue(GetPropertyInfo(property).Name, out Option? o))
        {
            var value = context.ParseResult.GetValueForOption(o);

            if (value is TValue v)
            {
                return v;
            }
            else
            {
                return default!;
            }
        }
        else
        {
            throw new InvalidOperationException($@"Option not found for property ""{info.Name}""");
        }
    }

    private static PropertyInfo GetPropertyInfo<TValue>(Expression<Func<TOptions, TValue>> property)
    {
        if (property.Body is MemberExpression expr && expr.Member is PropertyInfo info)
        {
            return info;
        }
        else
        {
            throw new ArgumentException("Invalid accessor", nameof(property));
        }
    }
}