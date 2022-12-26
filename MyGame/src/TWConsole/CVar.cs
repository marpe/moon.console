using System.Reflection;

namespace MyGame.TWConsole;

public class CVar
{
    private readonly MemberInfo _memberInfo;
    public readonly object? DefaultValue;
    public readonly string Key;
    public bool SaveToCfg;

    public CVar(string key, FieldInfo field, bool saveToCfg) : this(key, field, field.GetValue(null), saveToCfg)
    {
    }

    public CVar(string key, PropertyInfo prop, bool saveToCfg) : this(key, prop, prop.GetValue(null), saveToCfg)
    {
    }

    private CVar(string key, MemberInfo memberInfo, object? defaultValue, bool saveToCfg)
    {
        Key = key;
        _memberInfo = memberInfo;
        DefaultValue = defaultValue;
        SaveToCfg = saveToCfg;
    }

    public Type VarType
    {
        get
        {
            var type = _memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => throw new InvalidOperationException("Invalid cvar member info"),
            };
            return type;
        }
    }

    public string GetStringValue()
    {
        var value = GetValueRaw();
        return ConsoleUtils.ConvertToString(value);
    }

    public T GetValue<T>()
    {
        return (T)GetValueRaw()!;
    }

    public object? GetValueRaw()
    {
        var value = _memberInfo switch
        {
            FieldInfo field => field.GetValue(null),
            PropertyInfo prop => prop.GetValue(null),
            _ => throw new InvalidOperationException("Invalid cvar member info"),
        };
        return value;
    }

    private void SetValueInternal(object? value)
    {
        switch (_memberInfo)
        {
            case FieldInfo field:
            {
                field.SetValue(null, value);
                break;
            }
            case PropertyInfo prop:
            {
                prop.SetValue(null, value);
                break;
            }
            default:
                throw new InvalidOperationException("Invalid cvar member info");
        }
    }

    public void SetValue(string strValue)
    {
        if (VarType == typeof(string))
        {
            SetValueInternal(strValue);
        }
        else
        {
            var value = ConsoleUtils.ParseArg(VarType, strValue);
            SetValueInternal(value);
        }
    }
}
