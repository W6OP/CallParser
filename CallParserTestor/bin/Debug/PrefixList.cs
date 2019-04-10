using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CallParser
{
    internal enum PrefixType
    {
        pfNone = 0,
        pfDXCC = 1,
        pfProvince =2,
        pfStation = 3,
        pfDelDXCC = 4,
        pfOldPrefix = 5,
        pfNonDXCC = 6,
        pfInvalidPrefix = 7,
        pfDelProvince = 8,
        pfCity = 9
    };

    /// <summary>
    /// Load the list of prefixes from the prefix file.
    /// </summary>
    public class PrefixList
    {
        #region Constants

        private string DIGITS = "0123456789";
        private string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private string CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private Int32 HI_CHAR;

        #endregion

        // FUTURE USE IF NEEDED
       // private List<CallInfo> _CallInfoList;
        //internal List<CallInfo> CallInfoList
        //{
        //    get { return _CallInfoList; }
        //    set { _CallInfoList = value; }
        //}


        /// <summary>
        /// Path to the prefix file we are using.
        /// </summary>
        private string _PrefixFileName;
        public string PrefixFileName
        {
            get { return _PrefixFileName; }
            set { _PrefixFileName = value; }
        }

        /// <summary>
        /// Path to the call file we are using.
        /// </summary>
        private string _CallFileName;
        public string CallFileName
        {
            get { return _CallFileName; }
            set { _CallFileName = value; }
        }

        public List<PrefixInfo> PrefixEntryList { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PrefixList()
        {
            CHARS = DIGITS + LETTERS;
            HI_CHAR = CHARS.Length;

           // _Parser = new Parser();
        }

        #region Implementation

        /// <summary>
        /// http://stackoverflow.com/questions/868572/how-to-convert-object-to-liststring-in-one-line-of-c-sharp-3-0
        /// Read lines from a text file and load into a List<>
        /// </summary>
        /// <param name="fileName"></param>
        internal void LoadFiles()
        {
            if (File.Exists(_PrefixFileName))
            {
                List<string> prefixList = File.ReadAllLines(_PrefixFileName).Select(i => i.ToString()).ToList();

                // strip off first three lines that are comments
                // this statement says to copy all lines that don't start with #
                prefixList = prefixList.Where(x => x.IndexOf("#", 0, 1) == -1).ToList();

                //#InternalUse|Longitude|Latitude|Territory|+Prefix|-CQ|-ITU|-Continent|-TZ|-ADIF|-Province|-StartDate|-EndDate|-Mask|-Source|

                // COULD LOOK AT THE PREFIX TYPE FIRST AND NOT BOTHER WITH 
                // ALL THE CRAP I DON'T NEED


                // Merge the data sources using a named type. 
                // var could be used instead of an explicit type.
                IEnumerable<PrefixInfo> queryPrefixes =
                    from line in prefixList
                    let splitLine = line.Split('|')
                    select new PrefixInfo()
                    {
                        PrefixKind = DeterminePrefixType(splitLine[0]),
                        // convert hex string to int
                        Level = DetermineLevel(splitLine[0]),
                        Longitude = Convert.ToInt32(!string.IsNullOrEmpty(splitLine[1]) ? splitLine[1] : "0"),
                        Latitude = Convert.ToInt32(!string.IsNullOrEmpty(splitLine[2]) ? splitLine[2] : "0"),
                        Territory = splitLine[3],
                        Prefix = splitLine[4],
                        CQ = splitLine[5],
                        ITU = splitLine[6],
                        Continent = splitLine[7],
                        TZ = splitLine[8],
                        Adif = Convert.ToInt32(!string.IsNullOrEmpty(splitLine[9]) ? splitLine[9] : "0"),
                        Province = splitLine[10],
                        StartDate = splitLine[11],
                        EndDate = splitLine[12],
                        Mask = splitLine[13],
                        Source = splitLine[14]
                    };

                PrefixEntryList = queryPrefixes.ToList();

                CleanPrefixEntryList();
                BuildParentChildRelationships();
            }

            // FUTURE USE
            //BuildCallInfoList();
        }

        /// <summary>
        /// // FUTURE USE
        /// Build the list of calls ...
        /// </summary>
        //private void BuildCallInfoList()
        //{
        //    // probably ignore if error
        //    if (File.Exists(_CallFileName))
        //    {
        //        List<string> callList = File.ReadAllLines(_CallFileName).Select(i => i.ToString()).ToList();

        //        IEnumerable<CallInfo> queryCalls =
        //        from line in callList
        //        let splitLine = line.Split('=')
        //        select new CallInfo()
        //        {
        //            CallSign = splitLine[0],
        //            AdifCode = splitLine[1]
        //        };

        //        CallInfoList = queryCalls.ToList();
        //    }
        //}

        /// <summary>
        /// Delete all the items with invalid prefix types.
        /// </summary>
        private void CleanPrefixEntryList()
        {
            PrefixEntryList.RemoveAll(x => x.IsValidPefixType == false);
        }

        /// <summary>
        /// Look for the first parent, see if it has any children. Take those children
        /// and add them to the parents child collection. Then remove them from the 
        /// PrefixEntryList.
        /// </summary>
        private void BuildParentChildRelationships()
        {
            PrefixInfo parent = new PrefixInfo();

            foreach (PrefixInfo info in PrefixEntryList)
            {
                if (info.IsParent)
                {
                    parent = info;
                }
                else
                {
                    parent.Children.Add(info);
                }
            }

            PrefixEntryList.RemoveAll(x => x.IsParent == false);
        }

        /// <summary>
        /// Convert a string that is a hex representation of a number to an Int32.
        /// Look at the length of the string. If the length is greater than 2 take
        /// the last characters(s) after the first 2 and convert to an Int as from
        /// hex. IS LEVEL THE CHILDREN INDEX???
        /// </summary>
        /// <param name="levelIndicator"></param>
        /// <returns></returns>
        private Int32 DetermineLevel(string levelIndicator)
        {
            Int32 level = 0;
            string hexString = null;

            levelIndicator = CleanInput(levelIndicator);

            if (levelIndicator.Length > 2)
            {
                hexString = levelIndicator.Substring(2);
                level = Int32.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
            }

            return level;
        }

        /// <summary>
        /// Determine what type of prefix it is. Look at the length of the value.
        /// If it starts with -, L or M then get just the integer portion.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private PrefixType DeterminePrefixType(string value)
        {
            PrefixType prefixType = PrefixType.pfNone;
            string sValue = null;
            Int32 prefix = 0;

            if (value.Length > 0)
            {
                sValue = CleanInput(value);
                if (sValue.Length > 2)
                {
                    sValue = sValue.Substring(0, 2);
                    prefix = Convert.ToInt32(sValue);
                }
                else
                {
                    prefix = Convert.ToInt32(sValue);
                }

                prefixType = (PrefixType)prefix;
            }

            return prefixType;
        }

        /// <summary>
        /// Strip -,M,L from string
        /// </summary>
        /// <param name="strIn"></param>
        /// <returns></returns>
        internal string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings. 
            try
            {
                return Regex.Replace(strIn, @"[A-Za-z-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters,  
            // we should return Empty. 
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        //       private
        //  procedure LoadFromStrings(List: TStringList);
        //  function AddEntry: PPrefixEntry;
        //  procedure BuildRelations;
        //  function ParentOf(EntryNo: integer): integer;
        //  procedure BuildIndex;
        //  procedure AddToIndex(C1, C2: Char; Entry: PPrefixEntry);
        //public
        //  Entries: TPrefixArray;
        //  Count: integer;
        //  Index: array[1..HiChar, 1..HiChar] of PPrefixArray;
        //  procedure LoadFromFile(AFileName: TFileName);
        //  function Chop(var Str: string): string;
        //end;   

        #endregion


    } // end class
}
