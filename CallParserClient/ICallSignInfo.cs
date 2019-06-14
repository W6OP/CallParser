using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.CallParser
{
    public interface ICallSignInfo
    {

        void ParsePrefixFile(string filePath);

        IEnumerable<Hit> LookupCall(string call);

        IEnumerable<Hit> LookupCall(List<string> callSigns);


    } // end interface

    public enum PrefixKind
    {
        pfNone,
        pfDXCC,
        pfProvince,
        pfStation,
        pfDelDXCC,
        pfOldPrefix,
        pfNonDXCC,
        pfInvalidPrefix,
        pfDelProvince,
        pfCity
    }

    public enum CallSignFlag
    {
        cfInvalid,
        cfMaritime,
        cfPortable,
        cfSpecial,
        cfClub,
        cfBeacon,
        cfLotw,
        cfAmbigPrefix,
        cfQrp
    }

    /// <summary>
    /// Identify the type of character.
    /// </summary>
    public enum CharacterType
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
    /// Lightweight struct for the hit meta data. It is necessary to
    /// use this even though it has a significant time penalty because
    /// of the multi threading finding matches. Some objects would get
    /// updated on a different thread before they were added to the collection.
    /// This is a Value type!
    /// </summary>
    public struct Hit
    {
        public int Dxcc;  //dxcc_entity
        public int Wae;
        public string Iota;
        public string Wap;
        public string Cq;           //cq_zone
        public string Itu;          //itu_zone
        public string Admin1;
        public string Latitude;     //lat
        public string Longitude;    //long
        public CallSignFlag[] Flags;

        public string Continent;     //continent
        public string TimeZone;     //time_zone
        public string Admin2;
        public string Name;
        public string Qth;
        public string Comment;
        //public string CallbookEntry: Pointer; //to find out data sources

        public PrefixKind Kind;     //kind
        public string FullPrefix;   //what I determined the prefix to be - mostly for debugging
        public string MainPrefix;
        public string Country;       //country
        public string Province;     //province

        public string StartDate;
        public string EndDate;
        public bool IsIota;

        public string CallSign;
    }
}