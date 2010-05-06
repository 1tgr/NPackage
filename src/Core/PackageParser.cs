using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace NPackage.Core
{
    public static class PackageParser
    {
        private class NestedValue
        {
            private readonly int nesting;
            private readonly object value;

            public NestedValue(int nesting, object value)
            {
                this.nesting = nesting;
                this.value = value;
            }

            public int Nesting
            {
                get { return nesting; }
            }

            public object Value
            {
                get { return value; }
            }
        }

        public static void ParseYaml(TextReader reader, object obj)
        {
            Stack<NestedValue> stack = new Stack<NestedValue>();
            stack.Push(new NestedValue(0, obj));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int nesting = 0;
                while (line.Length > nesting && char.IsWhiteSpace(line, nesting))
                    nesting++;

                string s = line.Trim();
                if (s.Length == 0)
                    continue;

                string[] parts = s.Split(new[] { ':' }, 2);
                if (parts.Length < 2)
                    continue;

                NestedValue value = stack.Peek();
                while (nesting < value.Nesting)
                {
                    stack.Pop();
                    value = stack.Peek();
                }

                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value.Value);
                string name = parts[0].Replace("-", String.Empty);
                PropertyDescriptor property = properties[name];
                if (property == null)
                    continue;

                Type propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    Type[] genericArguments = propertyType.GetGenericArguments();
                    MethodInfo addMethod = propertyType.GetMethod("Add", genericArguments);
                    object dictionary = property.GetValue(value.Value);
                    object innerValue = Activator.CreateInstance(genericArguments[1]);
                    addMethod.Invoke(dictionary, new[] { parts[1].TrimStart(), innerValue });
                    stack.Push(new NestedValue(nesting + 1, innerValue));
                }
                else
                    property.SetValue(value.Value, parts[1].TrimStart());
            }
        }
    }
}