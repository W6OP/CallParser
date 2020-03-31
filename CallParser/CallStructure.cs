using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    /// <summary>
    /// 
    /// </summary>
    public class CallStructure
    {
        private readonly string[] SingleCharPrefixes = { "F", "G", "M", "I", "R", "W" };
        private readonly string[] RejectPrefixes = { "AG", "U", "R", "A", "B", "M", "P", "MM", "AM", "QR", "QRP", "QRPP", "LH", "LGT", "ANT", "WAP", "AAW", "FJL", "MOBILE" };

        public string Prefix { get; set; }
        public string BaseCall { get; set; }
        public string Suffix1 { get; set; }
        public string Suffix2 { get; set; }
        //public ComponentType ComponentType { get; set; } = ComponentType.Invalid;
        public CallStructureType CallStuctureType { get; set; } = CallStructureType.Invalid;
        private readonly Dictionary<string, List<int>> PortablePrefixes;

        public CallStructure(string callSign, Dictionary<string, List<int>> portablePrefixes)
        {
            
            PortablePrefixes = portablePrefixes;
            SplitCallSign(callSign);
        }

        public CallStructure()
        {
            CallStuctureType = CallStructureType.Invalid;
        }

        private void SplitCallSign(string callSign)
        {
            List<string> components = callSign.Split('/').ToList();

            if (components.Count > 3)
            {
                return;
            }

            // eliminate the most obvious invalid call signs
            List<(string call, StringTypes sType)> stringTypes =
                (components.Select(item => (call: item, sType: GetComponentType(item)))).ToList();

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
                        CallStuctureType = CallStructureType.Call;
                    }
                    else
                    {
                        CallStuctureType = CallStructureType.Invalid;
                    }
                    break;
                case 2:
                    ProcessComponents(components[0], components[1]);
                    break;
                case 3:
                    ProcessComponents(components[0], components[1], components[2]);
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
            ComponentType state = ComponentType.Invalid;
            ComponentType component0Type;
            ComponentType component1Type;

            component0Type = GetComponentType(component0, 1);
            component1Type = GetComponentType(component1, 2);

            BaseCall = component0;
            Prefix = component1;

            // ValidStructures = 'C#:CM:CP:CT:PC:';
            switch (state)
            {
                // if either is invalid short cicuit all the checks and exit immediately
                case ComponentType _ when component0Type == ComponentType.Invalid || component1Type == ComponentType.Invalid:
                    return;

                // CP 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix:
                    CallStuctureType = CallStructureType.CallPrefix;
                    return;

                // PC 
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign:
                    CallStuctureType = CallStructureType.PrefixCall;
                    return;

                // CC  ==> CP - check BU - BY - VU4 - VU7
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.CallSign:
                    if (component1.First() == 'B')
                    {
                        CallStuctureType = CallStructureType.CallPrefix;
                        return;
                    }
                    else if (component0.StartsWith("VU4") || component0.StartsWith("VU7"))
                    {
                        CallStuctureType = CallStructureType.CallPrefix;
                        return;
                    }
                    else
                    {
                        return;
                    }

                // CT
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Text:
                    CallStuctureType = CallStructureType.CallText;
                    return;

                // C#
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric:
                    CallStuctureType = CallStructureType.CallDigit;
                    return;

                // CM
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable:
                    CallStuctureType = CallStructureType.CallPortable;
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
                    CallStuctureType = CallStructureType.CallDigitPortable;
                    return;

                // C#T 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Numeric && component2Type == ComponentType.Text:
                    CallStuctureType = CallStructureType.CallDigitText;
                    return;

                // CMM 
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Portable:
                    CallStuctureType = CallStructureType.CallPortablePortable;
                        return;
                   
                // CMP
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Prefix:
                    CallStuctureType = CallStructureType.CallPortablePrefix;
                    return;

                // CMT
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Portable && component2Type == ComponentType.Text:
                    CallStuctureType = CallStructureType.CallPortableText;
                    return;

                // CPM
                case ComponentType _ when component0Type == ComponentType.CallSign && component1Type == ComponentType.Prefix && component2Type == ComponentType.Portable:
                    CallStuctureType = CallStructureType.CallPrefixPortable;
                    return;

                // PCM
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign && component2Type == ComponentType.Portable:
                    CallStuctureType = CallStructureType.CallPortable;
                    return;

                // PCT
                case ComponentType _ when component0Type == ComponentType.Prefix && component1Type == ComponentType.CallSign && component2Type == ComponentType.Text:
                    CallStuctureType = CallStructureType.PrefixCallText;
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// Just a quick test for grossly invalid call signs.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private StringTypes GetComponentType(string item)
        {
            switch (item)
            {
                //W6 OP -no spaces alowed
                case string _ when item.Any(char.IsWhiteSpace):
                    return StringTypes.Invalid;

                case string _ when string.IsNullOrEmpty(item):
                    return StringTypes.Invalid;

                // W$OP - no special characters allowed
                case string _ when item.Any(c => !char.IsLetterOrDigit(c)):
                    return StringTypes.Invalid;

                // call is too long
                case string _ when item.Length > 13:
                    return StringTypes.Invalid;

                default:
                    return StringTypes.Valid;
            }
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
            string[] validCallStructures = { "@#@@", "@#@@@", "@##@", "@##@@", "@##@@@", "@@#@", "@@#@@", "@@#@@@", "#@#@", "#@#@@", "#@#@@@", "#@@#@", "#@@#@@" }; // KH6Z
            string[] validPrefixes = { "@", "@@", "@@#", "@@#@", "@#", "@#@", "@##", "#@", "#@@", "#@#", "#@@#" };
            string[] validPrefixOrCall = { "@@#@", "@#@" };
            ComponentType state = ComponentType.Unknown;

            string pattern = BuildPattern(candidate);

            switch (state)
            {
                case ComponentType _ when position == 1 && candidate == "MM":
                    return ComponentType.Portable;

                case ComponentType _ when position == 1 && candidate.Length == 1:
                    return VerifyIfPrefix(candidate, position);

                case ComponentType _ when IsSuffix(candidate):
                    return ComponentType.Portable;

                case ComponentType _ when candidate.Length == 1:
                    if (candidate.All(char.IsDigit)) 
                    { return ComponentType.Numeric; }
                    else { return ComponentType.Text; }

                case ComponentType _ when IsText(candidate):
                    if (candidate.Length > 2) {
                        return ComponentType.Text;
                    }

                    if (VerifyIfPrefix(candidate, position) == ComponentType.Prefix)
                    {
                        return ComponentType.Prefix;
                    }
                    return ComponentType.Text;

                // this first case is somewhat redundant 
                case ComponentType _ when (validPrefixOrCall.Contains(pattern)):
                    // now determine if prefix or call
                    if (VerifyIfPrefix(candidate, position) == ComponentType.Prefix)
                    {
                        return ComponentType.Prefix;
                    }
                    else
                    {
                        return VerifyIfCallSign(candidate);
                    }

                case ComponentType _ when (validPrefixes.Contains(pattern) && VerifyIfPrefix(candidate, position) == ComponentType.Prefix):
                    return ComponentType.Prefix;

                case ComponentType _ when (validCallStructures.Contains(pattern) && VerifyIfCallSign(candidate) == ComponentType.CallSign):
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
        private bool CheckForPortablePrefix(string candidate)
        {
            if (SingleCharPrefixes.Contains(candidate))
            {
                return true;
            }
            return false;
        }

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
        private ComponentType VerifyIfPrefix(string candidate, int position)
        {
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

            if (PortablePrefixes.ContainsKey(candidate + "/"))
            {
                return ComponentType.Prefix;
            }

            return ComponentType.Text;
        }

        /// <summary>
        /// 
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
            catch (Exception ex)
            {
                var q = ex.Message;
                // bury exception
            }

            if (digits > 0 && digits <= 4)
            {
                if (candidate.Length < 6 && candidate.All(char.IsLetter))
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
        private string BuildPattern(string candidate)
        {
            string pattern = "";

            foreach (char item in candidate)
            {
                if (Char.IsLetter(item))
                {
                    pattern += "@";
                }

                if (char.IsDigit(item))
                {
                    pattern += "#";
                }

                if (char.IsPunctuation(item) || char.IsWhiteSpace(item))
                {
                    pattern += "?";
                }
            }

            return pattern;
        }


    } // end class
}
