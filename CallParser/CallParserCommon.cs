
/*
 CallParserCommon.cs
 CallParser
 
 Created by Peter Bourget on 6/9/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Contains common elements of the program.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace W6OP.CallParser
{
    /// <summary>
    /// State a Component may be.
    /// 
    /// </summary>
    public enum ComponentType : byte // Byte for reduced memory use.
    {
        CallSign,
        CallOrPrefix,
        Prefix,
        Unknown
    }

    public enum StringTypes
    {
        Numeric,
        Text,
        Invalid,
        Valid,
    }

    /// <summary>
    /// Valid kinds pf prefixes.
    /// </summary>
    public enum PrefixKind
    {
        [Description("pfNone")]
        None,
        [Description("pfDXCC")]
        DXCC,
        [Description("pfProvince")]
        Province,
        [Description("pfStation")]
        Station,
        [Description("pfDelDXCC")]
        DelDXCC,
        [Description("pfOldPrefix")]
        OldPrefix,
        [Description("pfNonDXCC")]
        NonDXCC,
        [Description("pfInvalidPrefix")]
        InvalidPrefix,
        [Description("pfDelProvince")]
        DelProvince,
        [Description("pfCity")]
        City
    }

    /// <summary>
    /// Flag to indicate the status of the call sign.
    /// </summary>
    public enum CallSignFlag
    {
        Invalid,
        Maritime,
        Portable,
        Special,
        Club,
        Beacon,
        Lotw,
        AmbigPrefix,
        Qrp
    }

    /// <summary>
    /// Identify the type of character.
    /// </summary>
    enum CharacterType
    {
        [Description("")]
        empty,
        [Description("#")]
        numeric,
        [Description("@")]
        alphabetical,
        [Description("?")]
        alphanumeric,
        [Description("-")]
        dash,
        [Description(".")]
        dot,
        [Description("/")]
        slash
    }

    /// <summary>
    /// https://stackoverflow.com/questions/49190830/is-it-possible-for-string-split-to-return-tuple
    /// </summary>
    //public static class Extensions
    //{
    //    public static void Deconstruct<T>(this IList<string> list, out (string, ComponentType) first, out (string, ComponentType) rest)
    //    {

    //        first = (list.Count > 0 ? list[0] : default, ComponentType.Unknown); // or throw
    //        rest = (list.Skip(1).ToString(), ComponentType.Unknown);
    //    }

    //    //public static void Deconstruct<(string, ComponentType)>(this IList<(string, ComponentType)> list, out (string, ComponentType) first, out (string, ComponentType) second, out IList<T> rest)
    //    //{
    //    //    // return (baseCall: tempComponents[0], callPrefix: tempComponents[0]);
    //    //    ComponentType componentType = ComponentType.Unknown;
    //    //    (string, ComponentType) f; // = ("", ComponentType.Unknown);

    //    //    f = (list.Count > 0 ? list[0] : "", ComponentType.Unknown); // or throw
    //    //    first = 
    //    //    //second = list.Count > 1 ? list[1] : default(T); // or throw
    //    //    //rest = list.Skip(2).ToList();
    //    //}

    //    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out T rest , out IList<T> more)
    //    {
    //        first = list.Count > 0 ? list[0] : default(T); // or throw
    //        second = list.Count > 1 ? list[1] : default(T); // or throw
    //        rest = list.Count > 2 ? list[2] : default(T); // or throw
    //        more = list.Skip(3).ToList();
    //    }

    //    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out T rest, out T more, out IList<T> evenmore)
    //    {
    //        first = list.Count > 0 ? list[0] : default(T); // or throw
    //        second = list.Count > 1 ? list[1] : default(T); // or throw
    //        rest = list.Count > 2 ? list[2] : default(T); // or throw
    //        more = list.Count > 3 ? list[3] : default(T); // or throw
    //        evenmore = list.Skip(4).ToList();
    //    }
    //}

    /// <summary>
    /// Admin Key
    /// </summary>
    public class Admin
    {
        public string AdminKey { get; set; }
        public CallSignInfo CallInfo { get; set; }

        public Admin(string admin, CallSignInfo hit)
        {
            AdminKey = admin;
            CallInfo = hit;
        }
    }

    /// <summary>
    /// Extension class to get the enum value from the description.
    /// </summary>
    internal static class EnumEx
    {
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            return default;
        }
    } // end class
}
