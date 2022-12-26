namespace MyGame.Utils;

public static class ReflectionUtils
{
    public static string GetDisplayName(Type? type)
    {
        if (type == null)
            return "Global";
        if (type.DeclaringType != null)
            return type.DeclaringType.Name + "." + type.Name;
        return type.Name;
    }
}
