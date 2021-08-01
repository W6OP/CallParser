using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    /// <summary>
    /// 
    /// </summary>
    internal class CallStructure
    {
        private readonly string[] SingleCharPrefixes = { "F", "G", "M", "I", "R", "W" };
      
        internal string Prefix { get; set; }
        internal string BaseCall { get; set; }
        internal string FullCall { get; set; }
        internal string Suffix1 { get; set; }
        internal string Suffix2 { get; set; }
        //internal string Pattern { get; set; }
        internal HashSet<CallSignFlags> CallSignFlags { get; set; }

        public CallStructureType CallStructureType { get; set; } = CallStructureType.Invalid;

        private readonly ConcurrentDictionary<string, List<PrefixData>> PortablePrefixes;

        internal CallStructure(string callSign, ConcurrentDictionary<string, List<PrefixData>> portablePrefixes)
        {

            PortablePrefixes = portablePrefixes;
            CallSignFlags = new HashSet<CallSignFlags>();
            SplitCallSign(callSign);
        }

        private void SplitCallSign(string callSign)
        {
            List<string> components = callSign.Split('/').ToList();

            FullCall = callSign;

            if (components.Count > 3)
            {
                return;
            }

            // eliminate the most obvious invalid call signs
            List<(string call, StringTypes sType)> stringTypes =
                components.Select(item => (call: item, sType: GetComponentType(item))).ToList();

            // if any portions are obviously invalid don't bother processing it
            if (stringTypes.Where(t => t.sType == StringTypes.Invalid)
                           .ToList().Count > 0)
            {
                return;
            }

            AnalyzeComponents(components);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="components"></param>
        private void AnalyzeComponents(List<string> components)
        {
            switch (components.Count)
            {
                case 0:
                    return;
                case 1:
                    if (VerifyIfCallSign(components[0]) == ComponentType.CallSign)
                    {
                        BaseCall = components[0];
                        CallStructureType = CallStructureType.Call;
                    }
                    else
                    {
                        CallStructureType = CallStructureType.Invalid;
                    }
                    break;
                case 2:
                    ProcessComponents(components[0], components[1]);
                    break;
                case 3:
                    ProcessComponents(components[0], components[1], components[2]);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// TODO: order these in order use for performance
        /// </summary>
        /// <param name="component0"></param>
        /// <param name="component1"></param>
        private void ProcessComponents(string component0, string component1)
        {
            ComponentType componentType = ComponentType.Invalid;
            ComponentType component0Type;
            ComponentType component1Type;

            component0Type = GetComponentType(component0, 1);
            component1Type = GetComponentType(component1, 2);

            if (component0Type == ComponentType.Unknown || component1Type == ComponentType.Unknown)
            {
                ResolveAmbiguities(component0Type, component1Type, out component0Type, out component1Type);
            }

            BaseCall = component0;
            Prefix = component1;

            // ValidStructures = 'C#:CM:CP:CT:PC:';
            switch (componentType)
            {
                // if either is invalid short cicuit all the checks and exit immediately
                case ComponentType _ when component0Type == ComponentType.Invalid || component1Type == ComponentType.Invalid:
                    return;

                // CP 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix:
                    CallStructureType = CallStructureType.CallPrefix;
                    return;

                // PC 
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign:
                    CallStructureType = CallStructureType.PrefixCall;
                    SetCallSignFlags(component0, "");
                    BaseCall = component1;
                    Prefix = component0;
                    return;
                
                    // PP - PrefixPortable
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Portable:
                    CallStructureType = CallStructureType.Invalid;
                    return;

                // CC  ==> CP - check BU - BY - VU4 - VU7
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallSign:
                    if (component1.First() == 'B')
                    {
                        CallStructureType = CallStructureType.CallPrefix;
                        SetCallSignFlags(component0, "");
                        return;
                    }
                    else if (component0.StartsWith("VU4") || component0.StartsWith("VU7"))
                    {
                        CallStructureType = CallStructureType.CallPrefix;
                        SetCallSignFlags(component1, "");
                        return;
                    }
                    else
                    {
                        return;
                    }

                // CT
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Text:
                    CallStructureType = CallStructureType.CallText;
                    SetCallSignFlags(component1, "");
                    return;

                // TC
                case ComponentType _ when component0Type == ComponentType.Text && component1Type == ComponentType.CallSign:
                    CallStructureType = CallStructureType.CallText;
                    BaseCall = component1;
                    Prefix = component0;
                    SetCallSignFlags(component1, "");
                    return;

                // C#
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric:
                    CallStructureType = CallStructureType.CallDigit;
                    SetCallSignFlags(component1, "");
                    return;

                // CM
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable:
                    SetCallSignFlags(component1, "");
                    CallStructureType = CallStructureType.CallPortable;
                    return;

                // PU
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.Unknown:
                    CallStructureType = CallStructureType.PrefixCall;
                    BaseCall = component1;
                    Prefix = component0;
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// Analyze each component and build a pattern of each type.
        /// Compare the final pattern to the CallStructureTypes allowed.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        private void ProcessComponents(string component0, string component1, string component2)
        {
            ComponentType state = ComponentType.Invalid;
            ComponentType component0Type;
            ComponentType component1Type;
            ComponentType component2Type;

            component0Type = GetComponentType(component0, 1);
            component1Type = GetComponentType(component1, 2);
            component2Type = GetComponentType(component2, 3);

            if (component0Type == ComponentType.Unknown || component1Type == ComponentType.Unknown)
            {
                ResolveAmbiguities(component0Type, component1Type, out component0Type, out component1Type);
            }

            BaseCall = component0;
            Prefix = component1;
            Suffix1 = component2;

            // ValidStructures = 'C#M:C#T:CM#:CMM:CMP:CMT:CPM:PCM:PCT:'
            switch (state)
            {
                // if either is invalid short cicuit all the checks and exit immediately
                case ComponentType _ when component0Type == ComponentType.Invalid || component1Type == ComponentType.Invalid || component2Type == ComponentType.Invalid:
                    return;

                // C#M 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric && component2Type == ComponentType.Portable:
                    CallStructureType = CallStructureType.CallDigitPortable;
                    SetCallSignFlags(component2, "");
                    return;

                // C#T 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric && component2Type == ComponentType.Text:
                    CallStructureType = CallStructureType.CallDigitText;
                    SetCallSignFlags(component2, "");
                    return;

                // CMM 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Portable:
                    CallStructureType = CallStructureType.CallPortablePortable;
                    SetCallSignFlags(component1, "");
                    return;

                // CMP
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Prefix:
                    BaseCall = component0;
                    Prefix = component2;
                    Suffix1 = component1;
                    CallStructureType = CallStructureType.CallPortablePrefix;
                    SetCallSignFlags(component1, "");
                    return;

                // CMT
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Text:
                    CallStructureType = CallStructureType.CallPortableText;
                    SetCallSignFlags(component1, "");
                    return;

                // CPM
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix && component2Type == ComponentType.Portable:
                    CallStructureType = CallStructureType.CallPrefixPortable;
                    SetCallSignFlags(component2, "");
                    return;

                // PCM
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign && component2Type == ComponentType.Portable:
                    BaseCall = component1;
                    Prefix = component0;
                    Suffix1 = component2;
                    CallStructureType = CallStructureType.PrefixCallPortable;
                    return;

                // PCT
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign && component2Type == ComponentType.Text:
                    BaseCall = component1;
                    Prefix = component0;
                    Suffix1 = component2;
                    CallStructureType = CallStructureType.PrefixCallText;
                    return;

                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Numeric:
                    BaseCall = component0;
                    Prefix = component2;
                    Suffix1 = component1;
                    SetCallSignFlags(component2, "");
                    CallStructureType = CallStructureType.CallDigitPortable;
                    return;

                default:
                    return;
            }
        }

        /// <summary> 
        ///resolve ambiguities
        ///FStructure:= StringReplace(FStructure, 'UU', 'PC', [rfReplaceAll]);
        ///FStructure:= StringReplace(FStructure, 'CU', 'CP', [rfReplaceAll]);
        ///FStructure:= StringReplace(FStructure, 'UC', 'PC', [rfReplaceAll]);
        ///FStructure:= StringReplace(FStructure, 'UP', 'CP', [rfReplaceAll]);
        ///FStructure:= StringReplace(FStructure, 'PU', 'PC', [rfReplaceAll]);
        ///FStructure:= StringReplace(FStructure, 'U', 'C', [rfReplaceAll]);
        /// </summary>
        /// <param name="component0Type"></param>
        /// <param name="component1Type"></param>
        private void ResolveAmbiguities(ComponentType componentType0, ComponentType componentType1, out ComponentType component0Type, out ComponentType component1Type)
        {
            switch (ComponentType.Invalid)
            {
                // UU --> PC
                case ComponentType _ when componentType0 == ComponentType.Unknown && componentType1 == ComponentType.Unknown:
                    component0Type = ComponentType.Prefix;
                    component1Type = ComponentType.CallSign;
                    return;

                // CU --> CP - I don't agree with this --> CT
                case ComponentType _ when componentType0 == ComponentType.CallSign && componentType1 == ComponentType.Unknown:
                    component0Type = ComponentType.CallSign;
                    component1Type = ComponentType.Text;
                    return;

                // UC --> PC - I don't agree with this and think it should be --> TC
                // UC --> PC does work for Heard Is though VK0H/MB5KET
                case ComponentType _ when componentType0 == ComponentType.Unknown && componentType1 == ComponentType.CallSign:
                    component0Type = ComponentType.Prefix;
                    component1Type = ComponentType.CallSign;
                    return;

                // UP --> CP
                case ComponentType _ when componentType0 == ComponentType.Unknown && componentType1 == ComponentType.Prefix:
                    component0Type = ComponentType.CallSign;
                    component1Type = ComponentType.Prefix;
                    return;

                // PU --> PC
                case ComponentType _ when componentType0 == ComponentType.Prefix && componentType1 == ComponentType.Unknown:
                    component0Type = ComponentType.Prefix;
                    component1Type = ComponentType.CallSign;
                    return;

                // U --> C
                case ComponentType _ when componentType0 == ComponentType.Unknown:
                    component0Type = ComponentType.CallSign;
                    component1Type = componentType1;
                    return;

                // U --> C
                case ComponentType _ when componentType1 == ComponentType.Unknown:
                    component1Type = ComponentType.CallSign;
                    component0Type = componentType0;
                    return;
            }

            component0Type = ComponentType.Unknown;
            component1Type = ComponentType.Unknown;
        }

        /// <summary>
        /// Set the call sign flags. 
        /// </summary>
        /// <param name="component"></param>
        private void SetCallSignFlags(string component1, string component2)
        {
            switch (component1)
            {
                case "R":
                    CallSignFlags.Add(CallParser.CallSignFlags.Beacon);
                    break;
                case "B":
                    CallSignFlags.Add(CallParser.CallSignFlags.Beacon);
                    break;
                case string _ when component1 == "P" && component2 == "QRP":
                    CallSignFlags.Add(CallParser.CallSignFlags.Portable);
                    CallSignFlags.Add(CallParser.CallSignFlags.Qrp);
                    break;
                case string _ when component1 == "QRP" && component2 == "P":
                    CallSignFlags.Add(CallParser.CallSignFlags.Portable);
                    CallSignFlags.Add(CallParser.CallSignFlags.Qrp);
                    break;
                case "P":
                    CallSignFlags.Add(CallParser.CallSignFlags.Portable);
                    break;
                case "M":
                    CallSignFlags.Add(CallParser.CallSignFlags.Portable);
                    break;
                case "MM":
                    CallSignFlags.Add(CallParser.CallSignFlags.Maritime);
                    break;
                case "QRP":
                    CallSignFlags.Add(CallParser.CallSignFlags.Qrp);
                    break;
                default:
                    CallSignFlags.Add(CallParser.CallSignFlags.Portable);
                    break;
            }
        }


        /// <summary>
        /// Just a quick test for grossly invalid call signs.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private StringTypes GetComponentType(string item)
        {
            return item switch
            {
                //W6 OP -no spaces alowed
                string _ when item.Any(char.IsWhiteSpace) => StringTypes.Invalid,
                string _ when string.IsNullOrEmpty(item) => StringTypes.Invalid,
                // W$OP - no special characters allowed
                string _ when item.Any(c => !char.IsLetterOrDigit(c)) => StringTypes.Invalid,
                // call is too long
                _ => StringTypes.Valid,
            };
        }

        /// <summary>
        /// //one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// ValidPrefixes = ':@:@@:@@#:@@#@:@#:@#@:@##:#@:#@@:#@#:#@@#:';
        /// ValidStructures = ':C:C#:C#M:C#T:CM:CM#:CMM:CMP:CMT:CP:CPM:CT:PC:PCM:PCT:';
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private ComponentType GetComponentType(string candidate, int position)
        {
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
            string[] validPrefixOrCall = { "@@#@", "@#@" };

            StringBuilder patternBuilder = BuildPattern(candidate);

            switch (ComponentType.Unknown)
            {
                case ComponentType _ when position == 1 && candidate == "MM":
                    return ComponentType.Prefix;

                case ComponentType _ when position == 1 && candidate.Length == 1:
                    return VerifyIfPrefix(candidate, position, patternBuilder);

                case ComponentType _ when IsSuffix(candidate):
                    return ComponentType.Portable;

                case ComponentType _ when candidate.Length == 1:
                    if (candidate.All(char.IsDigit))
                    { return ComponentType.Numeric; }
                    else { return ComponentType.Text; }

                case ComponentType _ when IsText(candidate):
                    if (candidate.Length > 2)
                    {
                        return ComponentType.Text;
                    }

                    if (VerifyIfPrefix(candidate, position, patternBuilder) == ComponentType.Prefix)
                    {
                        return ComponentType.Prefix;
                    }
                    return ComponentType.Text;

                // this first case is somewhat redundant 
                case ComponentType _ when validPrefixOrCall.Contains(patternBuilder.ToString()):
                    // now check if its a prefix, if not its a Call
                    if (VerifyIfPrefix(candidate, position, patternBuilder) != ComponentType.Prefix)
                    {
                        return ComponentType.CallSign;
                    }
                    else
                    {
                        if (VerifyIfCallSign(candidate) == ComponentType.CallSign)
                        {
                            return ComponentType.Unknown;
                        }

                        else
                        {
                            return ComponentType.Prefix;
                        } 
                    }
               
                case ComponentType _ when (validPrefixes.Contains(patternBuilder.ToString()) && VerifyIfPrefix(candidate, position, patternBuilder) == ComponentType.Prefix):
                    return ComponentType.Prefix;

                case ComponentType _ when (VerifyIfCallSign(candidate) == ComponentType.CallSign): //validCallStructures.Contains(pattern) && 
                    return ComponentType.CallSign;

                default:
                    if (!IsText(candidate))
                    {
                        return ComponentType.Unknown;
                    }
                    return ComponentType.Text;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        //private bool CheckForPortablePrefix(string candidate)
        //{
        //    if (SingleCharPrefixes.Contains(candidate))
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private bool IsText(string candidate)
        {
            // /1J
            if (candidate.Length == 2)
            {
                return true;
            }

            // /JOHN
            if (candidate.All(char.IsLetter))
            {
                return true;
            }

            // /599
            if (candidate.All(char.IsDigit))
            {
                return true;
            }

            if (candidate.Any(char.IsDigit) && candidate.Any(char.IsLetter))
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private bool IsSuffix(string candidate)
        {
            string[] validSuffixes = { "A", "B", "M", "P", "MM", "AM", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL" };

            if (validSuffixes.Contains(candidate))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Test if a candidate is truly a prefix.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private ComponentType VerifyIfPrefix(string candidate, int position, StringBuilder patternBuilder)
        {
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };

            //string pattern = BuildPattern(candidate);

            if (candidate.Length == 1)
            {
                switch (position)
                {
                    case 1:
                        if (SingleCharPrefixes.Contains(candidate))
                        {
                            return ComponentType.Prefix;
                        }
                        else
                        {
                            return ComponentType.Text;
                        }
                    default:
                        return ComponentType.Text;
                }
            }

            // only allocate stringbuilder if necessary
            //StringBuilder patternBuilder = BuildPattern(candidate);

            if (validPrefixes.Contains(patternBuilder.ToString()))
            {
                if (PortablePrefixes.ContainsKey(patternBuilder.Append("/").ToString()))
                {
                    return ComponentType.Prefix;
                }
                else
                {

                }
            }

            return ComponentType.Text;
        }

        /// <summary>
        /// one of "@","@@","#@","#@@" followed by 1-4 digits followed by 1-6 letters
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        internal ComponentType VerifyIfCallSign(string candidate)
        {
            int digits = 0;

            // strip prefix
            switch (candidate)
            {
                case string _ when candidate.Take(2).All(char.IsLetter): // "@@"
                    candidate = candidate.Remove(0, 2);
                    break;
                case string _ when candidate.Take(1).All(char.IsLetter): // "@"
                    candidate = candidate.Remove(0, 1);
                    break;
                case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Skip(1).Take(2).All(char.IsLetter): // "#@@"
                    candidate = candidate.Remove(0, 3);
                    break;
                case string _ when candidate.Take(1).All(char.IsDigit) && candidate.Skip(1).Take(1).All(char.IsLetter): // #@
                    candidate = candidate.Remove(0, 2);
                    break;
                default:
                    return ComponentType.Invalid;
            }

            try
            {
                // count letters and  digits
                while (candidate.Take(1).All(char.IsDigit))
                {
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        digits++;
                        candidate = candidate.Remove(0, 1);
                        if (candidate.Length == 0) { return ComponentType.Invalid; }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // bury exception
            }

            if (digits > 0 && digits <= 4)
            {
                if (candidate.Length <= 6 && candidate.All(char.IsLetter))
                {
                    return ComponentType.CallSign; // MAKE THIS COMPOSITE TYPE
                }
            }

            return ComponentType.Invalid;
        }

        /// <summary>
        /// Build a pattern that models the string passed in.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public StringBuilder BuildPattern(string candidate)
        {
            StringBuilder patternBuilder = new StringBuilder();

            foreach (char item in candidate)
            {
                if (char.IsLetter(item))
                {
                    patternBuilder.Append("@");
                }

                if (char.IsDigit(item))
                {
                    patternBuilder.Append("#");
                }

                if (char.IsPunctuation(item) || char.IsWhiteSpace(item))
                {
                    if (item == '/')
                    {
                        patternBuilder.Append("/");
                    }
                    else
                    {
                        //pattern += "?";
                        patternBuilder.Append(":");
                    }
                }
            }

            return patternBuilder;
        }

    } // end class
}
