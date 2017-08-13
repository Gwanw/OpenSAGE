﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenZH.Data.Ini.Parser
{
    partial class IniParser
    {
        private static readonly Dictionary<Type, Dictionary<string, Enum>> CachedEnumMap = new Dictionary<Type, Dictionary<string, Enum>>();

        private Dictionary<string, Enum> GetEnumMap<T>()
            where T : struct
        {
            var enumType = typeof(T);
            if (!CachedEnumMap.TryGetValue(enumType, out var stringToValueMap))
            {
                stringToValueMap = Enum.GetValues(enumType)
                    .Cast<Enum>()
                    .Distinct()
                    .Select(x => new { Name = GetIniName(enumType, x), Value = x })
                    .Where(x => x.Name != null)
                    .ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

                CachedEnumMap.Add(enumType, stringToValueMap);
            }
            return stringToValueMap;
        }

        public T ParseEnum<T>()
            where T : struct
        {
            var stringToValueMap = GetEnumMap<T>();

            var token = NextToken(IniTokenType.Identifier);

            if (stringToValueMap.TryGetValue(token.StringValue.ToUpper(), out var enumValue))
                return (T) (object) enumValue;

            throw new IniParseException($"Invalid value for type '{typeof(T).Name}': '{token.StringValue}'", token.Position);
        }

        public T ParseEnumFlags<T>()
            where T : struct
        {
            var stringToValueMap = GetEnumMap<T>();

            var result = (T) (object) 0;

            do
            {
                var token = NextToken(IniTokenType.Identifier);

                if (!stringToValueMap.TryGetValue(token.StringValue.ToUpper(), out var enumValue))
                    throw new IniParseException($"Invalid value for type '{typeof(T).Name}': '{token.StringValue}'", token.Position);

                // Ugh.
                result = (T) (object) ((int) (object) result | (int) (object) enumValue);
            } while (Current.TokenType != IniTokenType.EndOfLine);

            return result;
        }

        public BitArray<T> ParseEnumBitArray<T>()
            where T : struct
        {
            var stringToValueMap = GetEnumMap<T>();

            var result = new BitArray<T>();

            do
            {
                var token = NextToken(IniTokenType.Identifier);

                if (!stringToValueMap.TryGetValue(token.StringValue.ToUpper(), out var enumValue))
                    throw new IniParseException($"Invalid value for type '{typeof(T).Name}': '{token.StringValue}'", token.Position);

                // Ugh.
                result.Set((T) (object) enumValue, true);
            } while (Current.TokenType != IniTokenType.EndOfLine);

            return result;
        }

        private static string GetIniName(Type enumType, Enum value)
        {
            var field = enumType.GetTypeInfo().GetDeclaredField(value.ToString());
            var iniEnumAttribute = field.GetCustomAttribute<IniEnumAttribute>();
            return iniEnumAttribute?.Name;
        }
    }
}