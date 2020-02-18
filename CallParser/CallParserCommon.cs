/**
 * Copyright (c) 2019 Peter Bourget W6OP
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

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
