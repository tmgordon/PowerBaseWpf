using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace PowerBaseWpf.Helpers
{
    public class TemplateSettings
    {
        public int MaxTokenLoops { get; set; }
        public bool TrimWhitespace { get; set; }
        public bool RequireUniqueAlias { get; set; }
        public bool CancelOnTokenError { get; set; }
        public bool CancelOnConditionalError { get; set; }
        public bool ApplyTokensBeforeConditionals { get; set; }
        public bool ApplyTokensAfterConditionals { get; set; }
        public bool ReplaceStringFieldsOnly { get; set; }
        public string DomainName { get; set; }
    }

    public class NumberReplacement
    {
        public string Character { get; set; }
        public bool FirstAvailableNumber { get; set; }
        public bool NoNumberIsOne { get; set; }
        public int CheckForExistingUsersUpTo { get; set; }
        public bool OnlyAppendNumberIfRequired { get; set; }
    }
    
    public class PasswordSettings
    {
        public int MaxPasswordLength { get; set; }
        public int MinPasswordLength { get; set; }
        public List<CharacterSet> CharacterSets { get; set; }
        public string StartingString { get; set; }
        public bool ChangeMinMaxWhenRequired { get; set; }
        public bool UseWeightedSets { get; set; }
    }

    public class TokenReplacement
    {
        public string Replace { get; set; }
        public string ReplaceWith { get; set; }
    }

    public class ResultActions
    {
        public string Result { get; set; }
        public List<string> Actions { get; set; }
    }
    public class ResultAction
    {
        public string Result { get; set; }
        public string Action { get; set; }
    }
    public class ConditionalReplacements
    {
        public string Expression { get; set; }
        public List<ResultActions> ResultActions { get; set; }
    }
    public class ConditionalReplacement
    {
        public string Expression { get; set; }
        public ResultAction ResultAction { get; set; }
    }
    public class CharacterSet
    {
        private string _characters;

        #region Initialization
        public CharacterSet()
        {
        }

        public CharacterSet(string characters, bool starting)
        {
            MinIterations = 1;
            MaxIterations = 1;
            Characters = characters;
            CurIterations = 0;
            StartingCharacter = starting;
        }

        public CharacterSet(string characters)
        {
            MinIterations = 1;
            MaxIterations = 1;
            Characters = characters;
            CurIterations = 0;
            StartingCharacter = false;
        }

        public CharacterSet(int min, int max, string characters, bool starting = false)
        {
            MinIterations = min;
            MaxIterations = max;
            Characters = characters;
            CurIterations = 0;
            StartingCharacter = starting;
        }

        #endregion

        #region Public Variables

        public string Characters
        {
            get { return _characters; }
            set
            {
                _characters = value;
                CharArray = value.ToCharArray();
            }
        }

        public int MinIterations { get; set; }
        public int MaxIterations { get; set; }

        [JsonIgnore]
        public int CurIterations { get; set; }

        [JsonIgnore]
        public char[] CharArray { get; set; }
        public bool StartingCharacter { get; set; }

        #endregion
    }
}