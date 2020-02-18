// <copyright file="CallLookUpTest.cs" company="W6OP">Copyright ©  2020</copyright>
using System;
using System.Collections.Generic;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using W6OP.CallParser;

namespace W6OP.CallParser.Tests
{
    /// <summary>This class contains parameterized unit tests for CallLookUp</summary>
    [PexClass(typeof(CallLookUp))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class CallLookUpTest
    {
        /// <summary>Test stub for .ctor(PrefixFileParser)</summary>
        [PexMethod]
        public CallLookUp ConstructorTest(PrefixFileParser prefixFileParser)
        {
            CallLookUp target = new CallLookUp(prefixFileParser);
            return target;
            // TODO: add assertions to method CallLookUpTest.ConstructorTest(PrefixFileParser)
        }

        /// <summary>Test stub for IsNumeric(String)</summary>
        [PexMethod]
        public bool IsNumericTest([PexAssumeUnderTest]CallLookUp target, string value)
        {
            bool result = target.IsNumeric(value);
            return result;
            // TODO: add assertions to method CallLookUpTest.IsNumericTest(CallLookUp, String)
        }

        /// <summary>Test stub for LookUpCall(List`1&lt;String&gt;)</summary>
        [PexMethod]
        public IEnumerable<CallSignInfo> LookUpCallTest([PexAssumeUnderTest]CallLookUp target, List<string> callSigns)
        {
            IEnumerable<CallSignInfo> result = target.LookUpCall(callSigns);
            return result;
            // TODO: add assertions to method CallLookUpTest.LookUpCallTest(CallLookUp, List`1<String>)
        }

        /// <summary>Test stub for LookUpCall(String)</summary>
        [PexMethod]
        public IEnumerable<CallSignInfo> LookUpCallTest01([PexAssumeUnderTest]CallLookUp target, string callSign)
        {
            IEnumerable<CallSignInfo> result = target.LookUpCall(callSign);
            return result;
            // TODO: add assertions to method CallLookUpTest.LookUpCallTest01(CallLookUp, String)
        }
    }
}
