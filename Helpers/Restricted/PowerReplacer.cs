using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerBaseWpf.Helpers.Restricted;

namespace PowerBaseWpf.Helpers
{
    public class PowerReplacer
    {

        public PowerReplacer()
        {
            Initialize();
        }
        public PowerReplacer(object obj)
        {
            Initialize();
            AddObject(obj);
        }

        private void Initialize()
        {
            ReplacementDictionary = new ConcurrentDictionary<string, string>();
        }
        public ConcurrentDictionary<string, string> ReplacementDictionary { get; set; }

        public void StartClassReplacement(object instance)
        {

        }

        public string Replace(string original)
        {
            var newValue = original;

            //Check for DateTime token
            var regex = Regex.Matches(newValue, $@"({{[L,U]?{{)(DateTime[:,-,\s]?)([^}}]*)(}}}})");

            foreach (Match match in regex)
            {
                var pattern = match.Groups[3].Value;
                var formattedDate = DateTime.Now.ToString(pattern);
                var capitalCheck = Regex.Match(match.Groups[1].Value, $@"({{)([L,U])({{)");
                if (capitalCheck.Success)
                {
                    if (capitalCheck.Groups[2].Value == "L")
                    {
                        formattedDate = formattedDate.ToLower();
                    }
                    if (capitalCheck.Groups[2].Value == "U")
                    {
                        formattedDate = formattedDate.ToUpper();
                    }
                }
                ReplacementDictionary[match.Groups[0].Value] = formattedDate;
            }

            // Check if field tokens are available, if not run replacement with current dictionary entries and return result. 
            if (Fields == null || Fields.Count < 1)
            {
                return ReplacementDictionary.Aggregate(newValue, (current, item) => current.Replace(item.Key, item.Value));
            }

            //Check if value contains a field token
            foreach (var field in Fields)
            {
                regex = Regex.Matches(newValue, $@"({{[L,U]?{{)(\[\d+\])?({field.FieldName})(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})");
                foreach (Match match in regex)
                {
                    // value contains token for this field. 
                    var upper = false;
                    var lower = false;
                    var capitalCheck = Regex.Match(match.Groups[1].Value, $@"({{)([L,U])({{)");
                    if (capitalCheck.Success)
                    {
                        if (capitalCheck.Groups[2].Value == "L") lower = true;
                        if (capitalCheck.Groups[2].Value == "U") upper = true;
                    }
                    var fSubstringCheck = Regex.Match(match.Groups[2].Value, $@"(\[\d+\])");
                    var fSubstring = false;
                    Int32 fSub = 0;
                    if (fSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(match.Groups[2].Value, "\\d+").Value, out fSub))
                        {
                            fSubstring = true;
                        }
                    }
                    var bSubstringCheck = Regex.Match(match.Groups[7].Value, $@"(\[\d+\])");
                    var bSubstring = false;
                    Int32 bSub = 0;
                    if (!fSubstring && bSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(match.Groups[7].Value, "\\d+").Value, out bSub))
                        {
                            bSubstring = true;
                        }
                    }
                    var fieldValue = field.FieldValues[0].Value;
                    var fieldTypeName = fieldValue.GetType().ToString();
                    var multiValueCheck = Regex.Match(fieldTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");

                    var splitRegex = Regex.Match(match.Groups[5].Value, $@"({{)(.)(}})");

                    if (splitRegex.Success)
                    {
                        var splitIndexRegex = Regex.Match(match.Groups[6].Value, $@"({{)(\d+)(}})");
                        var splitBy = splitRegex.Groups[2].Value;
                        Int32 spIndex;
                        if (Int32.TryParse(splitIndexRegex.Groups[2].Value, out spIndex))
                        {
                            if (multiValueCheck.Success && match.Groups[4].Value != "")
                            {
                                // Multi Value Field
                                Int32 vIndex;
                                if (!Int32.TryParse(Regex.Match(match.Groups[4].Value, "\\d+").Value, out vIndex)) continue;
                                dynamic val = fieldValue;
                                var strValue = val[vIndex].ToString();
                                if (!strValue.Contains(splitBy)) continue;
                                var sp = strValue.Split(new[] { splitBy }, StringSplitOptions.None);
                                if (sp.Length <= spIndex)
                                {
                                    spIndex = sp.Length - 1;
                                }

                                if (upper) sp[spIndex] = sp[spIndex].ToUpper();
                                if (lower) sp[spIndex] = sp[spIndex].ToLower();
                                if (fSubstring)
                                {
                                    if (fSub > sp[spIndex].Length) fSub = sp[spIndex].Length;
                                    sp[spIndex] = sp[spIndex].Substring(0, fSub);
                                }
                                if (bSubstring)
                                {
                                    if (bSub > sp[spIndex].Length) bSub = sp[spIndex].Length;
                                    sp[spIndex] = sp[spIndex].Substring(sp[spIndex].Length - bSub, bSub);
                                }

                                ReplacementDictionary[match.Groups[0].Value] = sp[spIndex];
                            }
                            else
                            {
                                // Single Value Field
                                if (fieldTypeName == "System.String")
                                {
                                    var strValue = fieldValue.ToString();
                                    if (!strValue.Contains(splitBy)) continue;
                                    var sp = strValue.Split(new[] { splitBy }, StringSplitOptions.None);
                                    if (sp.Length <= spIndex)
                                    {
                                        spIndex = sp.Length - 1;
                                    }
                                    if (upper) sp[spIndex] = sp[spIndex].ToUpper();
                                    if (lower) sp[spIndex] = sp[spIndex].ToLower();
                                    if (fSubstring) sp[spIndex] = sp[spIndex].Substring(0, fSub);
                                    if (bSubstring) sp[spIndex] = sp[spIndex].Substring(sp[spIndex].Length - bSub, bSub);

                                    ReplacementDictionary[match.Groups[0].Value] = sp[spIndex];
                                }

                            }
                        }
                    }
                    else
                    {
                        if (multiValueCheck.Success && match.Groups[4].Value != "")
                        {
                            // Multi Value Field
                            Int32 vIndex;
                            if (!Int32.TryParse(Regex.Match(match.Groups[4].Value, "\\d+").Value, out vIndex)) continue;
                            dynamic val = fieldValue;
                            var strValue = val[vIndex].ToString();

                            if (upper) strValue = strValue.ToUpper();
                            if (lower) strValue = strValue.ToLower();
                            if (fSubstring)
                            {
                                if (fSub > strValue.Length) fSub = strValue.Length;
                                strValue = strValue.Substring(0, fSub);
                            }
                            if (bSubstring)
                            {
                                if (bSub > strValue.Length) bSub = strValue.Length;
                                strValue = strValue.Substring(strValue.Length - bSub, bSub);
                            }
                            ReplacementDictionary[match.Groups[0].Value] = strValue;
                        }
                        else
                        {
                            // Single Value Field
                            if (fieldTypeName == "System.String")
                            {
                                var strValue = fieldValue.ToString();
                                if (upper) strValue = strValue.ToUpper();
                                if (lower) strValue = strValue.ToLower();
                                if (fSubstring) strValue = strValue.Substring(0, fSub);
                                if (bSubstring) strValue = strValue.Substring(strValue.Length - bSub, bSub);

                                ReplacementDictionary[match.Groups[0].Value] = strValue;
                            }
                        }
                    }
                }
            }
            return ReplacementDictionary.Aggregate(newValue, (current, item) => current.Replace(item.Key, item.Value));
        }

        public void AddToken(TokenReplacement token)
        {
            Fields.Add(new DeconstructedField()
            {
                FieldType = token.ReplaceWith.GetType(),
                FieldName = token.Replace,
                FieldTypeName = token.ReplaceWith.GetType().ToString(),
                ReplaceComplete = true,
                FieldValues = new List<KeyValuePair<string, object>>()
                {
                    new KeyValuePair<string, object>(token.Replace, token.ReplaceWith)
                },
            });
        }

        public void AddObject(object obj)
        {
            if (obj == null) return;
            var inputType = obj.GetType();
            var properties = inputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            foreach (var uItem in properties)
            {
                var skip = (NoReplacementAttribute[]) uItem.GetCustomAttributes(typeof(NoReplacementAttribute), false);
                if (skip.Length == 0)
                {
                    var fieldName = uItem.Name; // FirstName, LastName, MiddleInitial, etc.
                    var fieldValue = uItem.GetValue(obj); // Tom, Gordon, M, etc. (not a string yet)
                    if (fieldValue == null) continue;
                    var fieldTypeName = fieldValue.GetType().ToString();
                    Fields.Add(new DeconstructedField()
                    {
                        FieldType = fieldValue.GetType(),
                        FieldTypeName = fieldTypeName,
                        FieldName = fieldName,
                        ReplaceComplete = true,
                        FieldValues = new List<KeyValuePair<string, object>>()
                        {
                            new KeyValuePair<string, object>(fieldName, fieldValue)
                        },
                    });
                }
            }
        }

        private ObservableCollection<DeconstructedField> Fields { get; set; } = new ObservableCollection<DeconstructedField>();

        public bool ContainsValidToken(string value, IReflect type)
        {
            if (value == null) return false;

            if (type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Any(t => Regex.Match(value, $@"({{[L,U]?{{)(\[\d+\])?({t.Name})(\[\d+\])?(\..*)?(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})").Success))
            {
                return true;
            }

            if (!ContainsTokenSyntax(value)) return false;
            var token = GetTokens(value);
            if (token.Count == 0) return false;
            foreach (var item in ReplacementDictionary)
            {
                for (var i = 0; i < token.Count; i++)
                {
                    if (item.Key == token[i].Value) return true;
                }
            }

            return false;

        }
        public bool ContainsTokenSyntax(string value)
        {
            return Regex.Match(value ?? "NoMatch", $@"({{[L,U]?{{)(\[\d+\])?(.*)(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})").Success;
        }
        private MatchCollection GetTokens(string value)
        {
            return Regex.Matches(value ?? "NoMatch", $@"({{[L,U]?{{)(\[\d+\])?(.*)(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})");
        }
        private List<string> GetTokenList(string value)
        {
            var matches = GetTokenMatches(value);

            return (from Match match in matches select match.Groups[0].Value).ToList();
        }
        private MatchCollection GetTokenMatches(string value)
        {
            return Regex.Matches(value ?? "NoMatch", $@"({{[L,U]?{{)(\[\d+\])?(.*)(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})");
        }
    }
}
