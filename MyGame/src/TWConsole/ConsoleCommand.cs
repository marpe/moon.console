namespace MyGame.TWConsole;

public struct ConsoleCommandArg
{
    public string Name;
    public object? DefaultValue;
    public bool HasDefaultValue;
    public Type Type;

    public ConsoleCommandArg(string name, bool hasDefaultValue, object? defaultValue, Type type)
    {
        Name = name;
        HasDefaultValue = hasDefaultValue;
        DefaultValue = defaultValue;
        Type = type;
    }

    public string GetDescription()
    {
        if (HasDefaultValue)
            return $"{Name}, default: {ConsoleUtils.Colorize(DefaultValue)}^1";
        return $"{Name}";
    }
}

public class ConsoleCommand : IComparable<ConsoleCommand>
{
    public delegate void ConsoleCommandHandler(TWConsole console, ConsoleCommand cmd, string[] args);

    public string[] Aliases;
    public ConsoleCommandArg[] Arguments;
    public string Description;

    public ConsoleCommandHandler Handler;
    public bool IsCVar;
    public string Key;

    public ConsoleCommand(string key, string description, ConsoleCommandHandler handler, ConsoleCommandArg[] args, string[] aliases, bool isCVar)
    {
        Key = key;
        Handler = handler;
        Description = description;
        Arguments = args;
        Aliases = aliases;
        IsCVar = isCVar;
    }

    public int CompareTo(ConsoleCommand? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        if (IsCVar && !other.IsCVar) return 1;
        if (!IsCVar && other.IsCVar) return -1;

        if ((Key.StartsWith('+') || Key.StartsWith('-')) && (other.Key.StartsWith('+') || other.Key.StartsWith('-')))
            return string.Compare(Key[1..], other.Key[1..], StringComparison.Ordinal);

        return string.Compare(Key, other.Key, StringComparison.Ordinal);
    }

    public string PrettyPrint(bool includeArgs)
    {
        var cmdArgs = string.Empty;
        if (includeArgs && Arguments.Length > 0)
        {
            var formattedArgs = Arguments.Select(x => x.GetDescription());
            var args = string.Join(", ", formattedArgs);
            cmdArgs = $" ^1[{args}]^0";
        }

        var cmdDescription = string.Empty;
        if (!string.IsNullOrWhiteSpace(Description))
            cmdDescription = $": {Description}";
        return $"^6{Key}{cmdArgs}^0{cmdDescription}";
    }
}
