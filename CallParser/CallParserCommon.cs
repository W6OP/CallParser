
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
        Text,
        Numeric,
        Portable,
        //PortablePrefix, // indicates a portable prefix for a quicker lookup
        Unknown,
        Invalid,
        Valid
    }

    public enum StringTypes
    {
        Numeric,
        Text,
        Invalid,
        Valid,
    }

    /// <summary>
    /// ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
    /// </summary>
    public enum CallStructureType
    {
        [Description("C")]
        Call,
        [Description("C#")]
        CallDigit,
        [Description("C#M")]
        CallDigitPortable,
        [Description("C#T")]
        CallDigitText,
        [Description("CM")]
        CallPortable,
        [Description("CM#")]
        CallPortableDigit,
        [Description("CMM")]
        CallPortablePortable,
        [Description("CMP")]
        CallPortablePrefix,
        [Description("CMT")]
        CallPortableText,
        [Description("CP")]
        CallPrefix,
        [Description("CPM")]
        CallPrefixPortable,
        [Description("CT")]
        CallText,
        [Description("PC")]
        PrefixCall,
        [Description("PCM")]
        PrefixCallPortable,
        [Description("PCT")]
        PrefixCallText,
        [Description("Invalid")]
        Invalid
    }

    /// <summary>
    /// Valid kinds pf prefixes. These have int values as
    /// I need to rank them at times to resolve conflicts.
    /// </summary>
    public enum PrefixKind
    {
        [Description("pfNone")]
        None = 0,
        [Description("pfDXCC")]
        DXCC = 20,
        [Description("pfProvince")]
        Province = 19,
        [Description("pfStation")]
        Station = 18,
        [Description("pfCity")]
        City = 17,
        [Description("pfDelDXCC")]
        DelDXCC = 16,
        [Description("pfOldPrefix")]
        OldPrefix = 15,
        [Description("pfNonDXCC")]
        NonDXCC = 14,
        [Description("pfInvalidPrefix")]
        InvalidPrefix = 13,
        [Description("pfDelProvince")]
        DelProvince = 12
    }

    /// <summary>
    /// Flag to indicate the status of the call sign.
    /// </summary>
    [Flags]
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
}

namespace W6OP.CallParser
{
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
    }
    // end class
}