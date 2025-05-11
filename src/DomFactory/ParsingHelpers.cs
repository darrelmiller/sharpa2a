using System.Text.Json;

namespace DomFactory
{
    public static class ParsingHelpers
    {
        public static void ParseMap<T>(JsonElement node, T manifestDocument, FixedFieldMap<T> handlers, ValidationContext context)
        {
            foreach (var element in node.EnumerateObject())
            {
                var nodeName = context.NodeName + "/" + element.Name;
  
                var handler = FindHandler(element, handlers, context);
                if (handler is not null)
                {
                    try
                    {
                        var oldNodeName = context.NodeName;
                        context.NodeName += $"/{element.Name}";
                        handler(context, manifestDocument, element);
                        context.NodeName = oldNodeName;
                    }
                    catch (ProblemException e)
                    {
                        context.AddProblem(e.Problem);
                    }
                    catch (InvalidOperationException e)
                    {
                        context.AddProblem(new Problem
                        {
                            Rule = new InvalidJsonSemanticsRule(),
                            ProblemValues = [element.Name, element.Value.ToString(), e.Message],
                            Path = nodeName
                        });
                    }
                }
                else
                {

                    context.AddProblem(new Problem
                    {
                        Rule = new UnrecognizedMemberRule(),
                        ProblemValues = [element.Name, element.Value.ToString()],
                        Path = nodeName
                    });
                }
            }

        }

        public static Action<ValidationContext, T, JsonProperty>? FindHandler<T>(JsonProperty property, FixedFieldMap<T> handlers, ValidationContext context)
        {
            string memberName = property.Name;
            foreach (var handlerEntry in handlers)
            {
                if (handlerEntry.Key.MemberName == memberName && handlerEntry.Key.SupportedVersions.Contains(context.Version))
                {
                    return handlerEntry.Value;
                }
            }
            return null;
        }


        public static TEnum ParseDiscriminator<TEnum>(JsonElement v, string memberName, string objectName, ValidationContext context) where TEnum : struct, Enum
        {
            // Test if the member exists.
            if (v.TryGetProperty(memberName, out var value))
            {
                // Test if the value can be parsed into provided enum.
                if (Enum.TryParse<TEnum>(value.GetString(), out var result))
                {
                    return result;
                }
                else
                {
                    var allowedValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
                    context.AddProblem(new Problem
                    {
                        Rule = new InvalidTypeDiscriminatorRule(),
                        ProblemValues = [value.GetString(), objectName, context.Version, allowedValues]
                    });
                    return default;
                }
            }
            else
            {
                context.AddProblem(new Problem
                {
                    Rule = new MissingTypeDiscriminatorRule(),
                    ProblemValues = [memberName, objectName]
                });
                return default;
            }
        }

        public static TEnum ParseEnums<TEnum>(JsonElement v, string memberName, string objectName, ValidationContext context) where TEnum : struct, Enum
        {
            if (Enum.TryParse(v.GetString(), ignoreCase: true, out TEnum result))
            {
                return result;
            }
            else
            {
                var allowedValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
                context.AddProblem(new Problem
                {
                    Rule = new InvalidTypeDiscriminatorRule(),
                    ProblemValues = [v.GetString(), objectName, context.Version, allowedValues]
                });
                return default;
            }
        }

        // add context to check versions
        public static string GetString(JsonElement v)
        {
            return v.GetString() ?? throw new InvalidOperationException("Expected a string value.");
        }
        public static List<T> GetList<T>(JsonElement v, Func<JsonElement, ValidationContext, T> load, ValidationContext context)
        {
            var list = new List<T>();
            var index = 0;
            var oldNodeName = context.NodeName;
            foreach (var item in v.EnumerateArray())
            {
                context.NodeName += $"/{index++}";
                try
                {
                    list.Add(load(item, context));
                }
                catch (ProblemException e)
                {
                    context.AddProblem(e.Problem);
                }

                context.NodeName = oldNodeName;
            }
            return list;
        }

        public static Dictionary<string, T> GetMap<T>(JsonElement v, Func<JsonElement, ValidationContext, T> load, ValidationContext context)
        {
            var map = new Dictionary<string, T>();
            var oldNodeName = context.NodeName;
            foreach (var item in v.EnumerateObject())
            {
                context.NodeName += $"/{item.Name}";
                map.Add(item.Name, load(item.Value, context));
                context.NodeName = oldNodeName;
            }
            return map;
        }

        public static List<string> GetListOfString(JsonElement v)
        {
            var list = new List<string>();
            foreach (var item in v.EnumerateArray())
            {
                var value = item.GetString();
                if (value != null)
                    list.Add(value);
            }
            return list;
        }
    }

    public class FixedFieldMap<T> : Dictionary<HandlerKey, Action<ValidationContext, T, JsonProperty>>
    {
        public FixedFieldMap()
        {

        }
    }
}
