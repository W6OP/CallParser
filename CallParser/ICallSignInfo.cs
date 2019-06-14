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
    public interface ICallSignInfo
    {

        void ParsePrefixFile(string filePath);

        IEnumerable<Hit> LookupCall(string call);

        IEnumerable<Hit> LookupCall(List<string> callSigns);


    } // end interface
}
