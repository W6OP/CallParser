
/*
 CallParserCommon.cs
 CallParser
 
 Created by Peter Bourget on 6/9/19.
 Copyright © 2019 Peter Bourget W6OP. All rights reserved.
 
 Description: Contains common elements of the program.
 */
using System;
using System.ComponentModel;

namespace W6OP.CallParser
{
    /// <summary>
    /// Allows Tri State Bool
    /// </summary>
    public enum TriState : byte // Byte for reduced memory use.
    {
        CallSign,
        Prefix,
        CallOrPrefix,
        None
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
