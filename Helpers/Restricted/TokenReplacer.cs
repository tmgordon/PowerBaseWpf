using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PowerBaseWpf.Helpers.Restricted
{
    public class TokenReplacer : INotifyPropertyChanged
    {

        #region Public Objects

        public object InputObject
        {
            get { return _inputObject; }
            set
            {
                if (_inputObject != value)
                {
                    _inputObject = value;
                    NotifyPropertyChanged("InputObject");
                }
            }
        }

        private Type InputType { get; set; }
        public ConcurrentDictionary<string, string> ReplacementDictionary { get; set; }
        public ConcurrentDictionary<string, UniqueField> UniqueFieldsDictionary { get; set; }
        public Dictionary<string, string> ErrorDictionary { get; set; }
        public List<KeyValuePair<string, IdentityType>> UniqueFields { get; set; }
        public AdObjectCheck AdObjectCheck { get; set; }
        public ObservableCollection<DeconstructedField> InputFields { get; set; }
        public TemplateSettings TemplateSettings { get; set; }
        public List<NumberReplacement> NumberReplacements { get; set; }
        public string DomainName { get; set; }
        #endregion

        #region Private Objects

        private readonly List<string> _knownStandardTypes = new List<string>()
        {
            "System.String",
            "System.Char",
            "System.Boolean",
            "System.Int32",
            "System.Int64",
            "System.String[]",
            "System.Char[]",
            "System.Boolean[]",
            "System.Int32[]",
            "System.Int64[]",
            "System.DateTime",
            "System.TimeSpan"
        };

        private readonly List<string> _knownCollectionTypes = new List<string>()
        {
            "System.Collections.Generic.List",
            "System.Collections.ObjectModel.ObservableCollection",
            "System.Collections.Generic.KeyValuePair"
        };


        private readonly List<string> _ignoreFields = new List<string>()
        {
            "TemplateSettings",
            "MemberOf"
        };

        private object _inputObject;
        #endregion

        #region PropertyChanged Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void InputFieldsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {

            if (notifyCollectionChangedEventArgs.NewItems != null && notifyCollectionChangedEventArgs.NewItems.Count > 0)
            {
                foreach (var item in notifyCollectionChangedEventArgs.NewItems)
                {
                    var field = (DeconstructedField) item;
                    var name = field.FieldName;
                    var info = field.FieldInfo;
                    var type = field.FieldType;
                    var typeName = field.FieldTypeName;
                    var tokens = field.ReplaceComplete == false;
                    var values = field.FieldValues;

                    foreach (var val in values)
                    {
                        if (val.Value == null) continue;
                        var property = val.Key;
                        var valType = val.Value.GetType().ToString();
                        var propertyValue = val.Value;
                        if (propertyValue != info?.GetValue(InputObject))
                        {
                            Reflector.SetValue(InputObject, property, propertyValue);
                        }
                    }
                }
            }
        }


        #endregion

        #region Constructors & Initialization

        public TokenReplacer(TemplateSettings settings, List<KeyValuePair<string, IdentityType>> uniqueFields)
        {
            TemplateSettings = settings;
            UniqueFields = uniqueFields;
            Initialize();
        }

        public TokenReplacer(TemplateSettings settings)
        {
            TemplateSettings = settings;
            Initialize();
        }

        public TokenReplacer()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (TemplateSettings == null)
            {
                UseDefaultSettings();
            }
            if (UniqueFields == null)
            {
                UseDefaultUniqueFields();
            }
            AdObjectCheck = new AdObjectCheck();
            UniqueFieldsDictionary = new ConcurrentDictionary<string, UniqueField>();
            ReplacementDictionary = new ConcurrentDictionary<string, string>();
            ErrorDictionary = new Dictionary<string, string>();
            PopulateUniqueFieldsDictionary();
            AddTokenToReplacementDictionary("DomainName", "neirelocation.com");
        }

        private void PopulateUniqueFieldsDictionary()
        {
            foreach (var field in UniqueFields)
            {
                UniqueFieldsDictionary[field.Key] = new UniqueField(field.Value, field.Key);
            }
        }

        private void UseDefaultUniqueFields()
        {
            UniqueFields = new List<KeyValuePair<string, IdentityType>>()
            {
            };
        }

        private void UseDefaultSettings()
        {
            TemplateSettings = new TemplateSettings()
            {
                ApplyTokensAfterConditionals = true,
                ApplyTokensBeforeConditionals = true,
                CancelOnConditionalError = true,
                CancelOnTokenError = true,
                MaxTokenLoops = 3,
                RequireUniqueAlias = true,
                TrimWhitespace = true,
            };

            if (NumberReplacements == null || NumberReplacements.Count == 0)
            {
                NumberReplacements = new List<NumberReplacement>()
                {
                    new NumberReplacement()
                    {
                        Character = "#",
                        CheckForExistingUsersUpTo = 10,
                        FirstAvailableNumber = true,
                        NoNumberIsOne = true,
                        OnlyAppendNumberIfRequired = true,
                    }
                };
            }
        }

        #endregion

        #region Public Methods

        public void AddTemplateTokens(List<TokenReplacement> tokens)
        {
            foreach (var token in tokens)
            {
                AddTokenToReplacementDictionary(token.Replace, token.ReplaceWith);
                AddedFields.Add(new DeconstructedField()
                {
                    FieldName = token.Replace,
                    FieldType = token.ReplaceWith.GetType(),
                    FieldTypeName = token.ReplaceWith.GetType().Name,
                    FieldValues = new List<KeyValuePair<string, object>>()
                    {
                        new KeyValuePair<string, object>(token.Replace, token.ReplaceWith)
                    },
                    ReplaceComplete = false,
                });
            }
        }

        public void StartReplacement(object input)
        {
            InputFields = new ObservableCollection<DeconstructedField>();
            InputFields.CollectionChanged += InputFieldsOnCollectionChanged;
            ReplacementDictionary = new ConcurrentDictionary<string, string>();
            foreach (var field in AddedFields)
            {
                AddTokenToReplacementDictionary(field.FieldName, field.FieldValues[0].Value.ToString());
            }
            GetInputValues(input);
            SetValues();
            var x = 0;
            while (CheckEachField() && x < TemplateSettings.MaxTokenLoops)
            {
                x++;
            }
        }

        
        public async Task StartAsyncReplacement(object input, CancellationToken token=default(CancellationToken))
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            InputFields = new ObservableCollection<DeconstructedField>();
            InputFields.CollectionChanged += InputFieldsOnCollectionChanged;
            ReplacementDictionary = new ConcurrentDictionary<string, string>();
            foreach (var field in AddedFields)
            {
                AddTokenToReplacementDictionary(field.FieldName, field.FieldValues[0].Value.ToString());
            }
            await Task.Run(() => GetInputValuesAsync(input), token);
            SetValues();
            var x = 0;
            while (await CheckEachFieldAsync() && x < TemplateSettings.MaxTokenLoops) x++;
        }
        #endregion

        #region Private Methods
        private void SetObject(object obj)
        {
            InputObject = obj;
            InputType = InputObject.GetType();
        }

        private async Task GetInputValuesAsync(object input)
        {
            SetObject(input);
            InputFields = new ObservableCollection<DeconstructedField>();
            InputFields.CollectionChanged += InputFieldsOnCollectionChanged;
            ErrorDictionary.Clear();

            if (InputObject == null) return;

            foreach (var uItem in InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                var skip = (NoReplacementAttribute[])uItem.GetCustomAttributes(typeof(NoReplacementAttribute), false);
                if (skip.Length == 0)
                {
                    await ImportProperty(uItem);
                }
            }
        }
        private void GetInputValues(object input)
        {
            SetObject(input);

            if (InputObject == null) return;
            foreach (var uItem in InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                var skip = (NoReplacementAttribute[]) uItem.GetCustomAttributes(typeof(NoReplacementAttribute), false);
                if (skip.Length != 0)
                {
                    continue;
                }

                var fieldName = uItem.Name; // FirstName, LastName, MiddleInitial, etc. 
                var fieldValue = uItem.GetValue(InputObject); // Tom, Gordon, M, etc. (not a string yet)
                if (fieldValue == null) continue;
                var fieldTypeName = fieldValue.GetType().ToString();
                var regex = Regex.Match(fieldTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");
                var Field = new DeconstructedField()
                {
                    FieldInfo = FieldInfo(fieldName),
                    FieldType = fieldValue.GetType(),
                    FieldTypeName = fieldTypeName,
                    FieldName = fieldName,
                    ReplaceComplete = false,
                };
                if (regex.Success)
                {
                    // Field is a collection
                    var baseType = regex.Groups[1].Value;
                    var subTypeCount = int.Parse(regex.Groups[3].Value);
                    var subTypes = regex.Groups[5].Value.Split(',');
                    var customObject = false;
                    foreach (var sub in subTypes)
                    {
                        if (_knownCollectionTypes.All(t => t != sub) && _knownStandardTypes.All(t => t != sub))
                        {
                            customObject = true;
                        }
                    }
                    if (customObject)
                    {
                        if (baseType != "System.Collections.Generic.KeyValuePair")
                        {
                            Field.FieldValues = new List<KeyValuePair<string, object>>();
                            dynamic val = fieldValue;
                            for (var i = 0; i < val.Count; i++)
                            {
                                var cvals = GetCollectionValues($"{fieldName}[{i}]", val[i]);
                                foreach (var cval in cvals)
                                {
                                    Field.FieldValues.Add(new KeyValuePair<string, object>(cval.Key, cval.Value));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (baseType == "System.Collections.Generic.KeyValuePair")
                        {
                            dynamic val = fieldValue;
                            for (var i = 0; i < val.Count; i++)
                            {
                                var k = val[i].Key;
                                var v = val[i].Value;
                                var fname = $"{fieldName}[{i}]";
                                var fkey = $"{fieldName}[{i}].Key";
                                var fval = $"{fieldName}[{i}].Value";
                                string fvReplace = ReplaceTokens(fval, v);
                                AddTokenToReplacementDictionary(fkey, k);
                                AddTokenToReplacementDictionary(fname, $"{k}:{fvReplace}");
                                val[i].Value = ConvertFromString(fvReplace, subTypes[1]);
                            }
                            Field.FieldValues = new List<KeyValuePair<string, object>>()
                            {
                                new KeyValuePair<string, object>($"{fieldName}", val),
                            };
                        }
                        else
                        {
                            dynamic val = fieldValue;
                            var tasks = new Task[val.Count];
                            for (int i = 0; i < val.Count; i++)
                            {
                                int task = i;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        if (val[task] == null) return;
                                        var fname = $"{fieldName}[{task}]";
                                        var unique = CheckUniqueField(fname);
                                        var replace = unique != null ? ReplaceUnique(fname, val[task].ToString(), unique.IdentityType) : ReplaceTokens(fname, val[task]);
                                        val[task] = ConvertFromString(replace, subTypes[0]);
                                    });
                            }
                            Task.WaitAll(tasks);
                            Field.FieldValues = new List<KeyValuePair<string, object>>()
                            {
                                new KeyValuePair<string, object>($"{fieldName}", val),
                            };
                        }
                    }
                }
                else
                {
                    if (_knownStandardTypes.All(t => t != fieldTypeName))
                    {
                        Field.FieldValues = GetCollectionValues(fieldName, fieldValue);
                    }
                    else
                    {
                        dynamic val = fieldValue;
                        var fname = $"{fieldName}";
                        var unique = CheckUniqueField(fname);
                        var replace = unique != null ? ReplaceUnique(fname, val, unique.IdentityType) : ReplaceTokens(fname, val);
                        val = ConvertFromString(replace, fieldTypeName);

                        Field.FieldValues = new List<KeyValuePair<string, object>>()
                        {
                            new KeyValuePair<string, object>(fname, val),
                        };
                    }
                }
                InputFields.Add(Field);
            }
        }
        private async Task ImportProperty(PropertyInfo cItem)
        {
            var fieldName = cItem.Name; // FirstName, LastName, MiddleInitial, etc. 


            if (_ignoreFields.Contains(fieldName)) return;
            var fieldValue = cItem.GetValue(InputObject); // Tom, Gordon, M, etc. (not a string yet)
            if (fieldValue == null) return;
            var fieldTypeName = fieldValue.GetType().ToString();
            var regex = Regex.Match(fieldTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");

            var Field = new DeconstructedField()
            {
                FieldInfo = FieldInfo(fieldName),
                FieldType = fieldValue.GetType(),
                FieldTypeName = fieldTypeName,
                FieldName = fieldName,
                ReplaceComplete = false,
            };
            if (regex.Success)
            {
                // Field is a collection
                var baseType = regex.Groups[1].Value;
                var subTypeCount = int.Parse(regex.Groups[3].Value);
                var subTypes = regex.Groups[5].Value.Split(',');
                var customObject = false;
                foreach (var sub in subTypes)
                {
                    if (_knownCollectionTypes.All(t => t != sub) && _knownStandardTypes.All(t => t != sub))
                    {
                        customObject = true;
                    }
                }
                if (customObject)
                {
                    if (baseType != "System.Collections.Generic.KeyValuePair")
                    {

                        if (baseType == "System.Collections.Generic.List")
                        {
                            Field.FieldValues = new List<KeyValuePair<string, object>>();
                            dynamic val = fieldValue;
                            var ss = "here";
                            var tasks = new Task[val.Count];
                            for (var i = 0; i < val.Count; i++)
                            {

                                int task = i;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        var cvals = GetCollectionValues($"{fieldName}[{task}]", val[task]);
                                        foreach (var cval in cvals)
                                        {
                                            Field.FieldValues.Add(new KeyValuePair<string, object>(cval.Key, cval.Value));
                                        }
                                    });
                            }
                            await Task.WhenAll(tasks);
                        }
                        else
                        {
                            Field.FieldValues = new List<KeyValuePair<string, object>>();
                            dynamic val = fieldValue;
                            var tasks = new Task[val.Count];
                            for (var i = 0; i < val.Count; i++)
                            {

                                int task = i;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        var cvals = GetCollectionValues($"{fieldName}[{task}]", val[task]);
                                        foreach (var cval in cvals)
                                        {
                                            Field.FieldValues.Add(new KeyValuePair<string, object>(cval.Key, cval.Value));
                                        }
                                    });
                            }
                            await Task.WhenAll(tasks);
                        }
                    }
                }
                else
                {
                    if (baseType == "System.Collections.Generic.KeyValuePair")
                    {
                        dynamic val = fieldValue;
                        var tasks = new Task[val.Count];
                        for (var i = 0; i < val.Count; i++)
                        {
                            int task = i;
                            tasks[task] = Task.Factory.StartNew(
                                () =>
                                {
                                    var k = val[task].Key;
                                    var v = val[task].Value;
                                    var fname = $"{fieldName}[{task}]";
                                    var fkey = $"{fieldName}[{task}].Key";
                                    var fval = $"{fieldName}[{task}].Value";
                                    string fvReplace = ReplaceTokens(fval, v);
                                    AddTokenToReplacementDictionary(fkey, k);
                                    AddTokenToReplacementDictionary(fname, $"{k}:{fvReplace}");
                                    val[task].Value = ConvertFromString(fvReplace, subTypes[1]);
                                });
                        }
                        await Task.WhenAll(tasks);
                        Field.FieldValues = new List<KeyValuePair<string, object>>()
                            {
                                new KeyValuePair<string, object>($"{fieldName}", val),
                            };
                    }
                    else
                    {
                        dynamic val = fieldValue;
                        var tasks = new Task[val.Count];
                        for (int i = 0; i < val.Count; i++)
                        {
                            int task = i;
                            tasks[task] = Task.Factory.StartNew(
                                () =>
                                {
                                    if (val[task] == null) return;
                                    var fname = $"{fieldName}[{task}]";
                                    var unique = CheckUniqueField(fname);
                                    var replace = unique != null ? ReplaceUnique(fname, val[task].ToString(), unique.IdentityType) : ReplaceTokens(fname, val[task]);
                                    val[task] = ConvertFromString(replace, subTypes[0]);
                                });
                        }
                        await Task.WhenAll(tasks);
                        Field.FieldValues = new List<KeyValuePair<string, object>>()
                            {
                                new KeyValuePair<string, object>($"{fieldName}", val),
                            };
                    }
                }
            }
            else
            {
                if (_knownStandardTypes.All(t => t != fieldTypeName))
                {
                    Field.FieldValues = GetCollectionValues(fieldName, fieldValue);
                }
                else
                {
                    var fname = $"{fieldName}";
                    var unique = CheckUniqueField(fname);
                    var replace = unique != null ? ReplaceUnique(fname, fieldValue.ToString(), unique.IdentityType) : ReplaceTokens(fname, fieldValue);
                    fieldValue = ConvertFromString(replace, fieldTypeName);
                    Field.FieldValues = new List<KeyValuePair<string, object>>()
                        {
                            new KeyValuePair<string, object>(fname, fieldValue),
                        };
                }
            }
            InputFields.Add(Field);
        }

        

        private FieldInfo FieldInfo(string fieldName)
        {
            return InputType.GetField($"<{fieldName}>k__Backingfield", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase) ??
                                    (InputType.GetField($"_{fieldName}", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase) ??
                                        InputType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase));
        }

        private async Task<bool> CheckEachFieldAsync()
        {

            var replacements = new List<bool>();
            foreach (var field in InputFields)
            {
                replacements.Add(await UpdateFieldAsync(field));
            }

            return replacements.Any(t => t);
        }

        private async Task<bool> UpdateFieldAsync(DeconstructedField field)
        {
            var replacements = false;
            var name = field.FieldName;
            var info = field.FieldInfo;
            var type = field.FieldType;
            var typeName = field.FieldTypeName;
            var complete = field.ReplaceComplete;
            var values = field.FieldValues;
            if (values == null) return false;
            if (complete) return false;

            var valuesTask = new Task[values.Count];
            for (int t = 0; t < values.Count; t++)
            {
                int i = t;
                valuesTask[i] = Task.Factory.StartNew(
                    () =>
                    {
                        if (values[i].Value == null) return;
                        var property = values[i].Key;
                        var propTypeName = values[i].Value.GetType().ToString();
                        var propValue = values[i].Value;

                        var regex = Regex.Match(propTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");
                        if (regex.Success && regex.Groups[5].Value.Contains("System.String") && regex.Groups[1].Value != "System.Collections.Generic.KeyValuePair")
                        {
                            dynamic vals = propValue;
                            var valueSet = false;
                            var tokens = false;
                            var tasks = new Task[vals.Count];
                            for (int n = 0; n < vals.Count; n++)
                            {
                                int task = n;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        if (vals[task] == null) return;
                                        var fname = $"{property}[{task}]";
                                        var unique = CheckUniqueField(name);
                                        if (unique != null && unique.VerifiedValues.ContainsKey(fname)) return;
                                        var replace = unique != null ? ReplaceUnique(fname, vals[task], unique.IdentityType) : ReplaceTokens(fname, vals[task]);
                                        if (!ContainsValidToken(replace) && replace != vals[task])
                                        {
                                            vals[task] = replace;
                                            valueSet = true;
                                            replacements = true;
                                            return;
                                        }
                                        if (ContainsValidToken(vals[task])) tokens = true;
                                        if ((string) replace == (string) vals[task]) return;
                                        vals[task] = replace;
                                        replacements = true;
                                    });
                            }
                            Task.WhenAll(tasks).Wait();
                            if (valueSet)
                            {
                                values[i] = new KeyValuePair<string, object>(values[i].Key, vals);
                                Reflector.SetValue(InputObject, $"{property}", vals);
                                //info.SetValue(InputObject, vals);
                            }
                            field.ReplaceComplete = tokens == false;
                            return;
                        }

                        if (propTypeName == "System.String")
                        {
                            dynamic val = propValue;
                            var fname = $"{property}";
                            var unique = CheckUniqueField(name);
                            if (unique != null && unique.VerifiedValues.ContainsKey(name))
                            {
                                field.ReplaceComplete = true;
                                return;
                            }
                            if (unique == null && !ContainsValidToken(val))
                            {
                                field.ReplaceComplete = true;
                                return;
                            }
                            var replace = unique != null ? ReplaceUnique(fname, val, unique.IdentityType) : ReplaceTokens(fname, val);
                            if (!ContainsValidToken(replace) && (string) replace != (string) val)
                            {
                                val = replace;
                                values[i] = new KeyValuePair<string, object>(values[i].Key, replace);
                                Reflector.SetValue(InputObject, property, val);
                                //info.SetValue(InputObject, val);
                                field.ReplaceComplete = true;
                                replacements = true;
                            }
                            else
                            {
                                if ((string) val != (string) replace)
                                {
                                    val = replace;
                                    replacements = true;
                                }
                                field.ReplaceComplete = ContainsValidToken(val) == false;
                            }
                            return;
                        }
                        if (TemplateSettings.ReplaceStringFieldsOnly) return;
                        if (propTypeName == "System.Boolean" || propTypeName == "System.Int32" || propTypeName == "System.Int64" || propTypeName == "System.Char")
                        {
                            var strValue = propValue.ToString();
                            var unique = CheckUniqueField(typeName);
                            if (unique != null && unique.VerifiedValues.ContainsKey(typeName))
                            {
                                field.ReplaceComplete = true;
                                return;
                            }
                            if (unique == null && !ContainsValidToken(strValue))
                            {
                                field.ReplaceComplete = true;
                                return;
                            }
                            var replace = unique != null ? ReplaceUnique(property, strValue, unique.IdentityType) : ReplaceTokens(property, strValue);
                            if (!ContainsValidToken(replace))
                            {
                                values[i] = new KeyValuePair<string, object>(values[i].Key, ConvertFromString(replace, propTypeName));
                                replacements = true;
                                info.SetValue(InputObject, replace);
                                field.ReplaceComplete = true;
                                return;
                            }
                            if (replace == strValue) return;
                            values[i] = new KeyValuePair<string, object>(values[i].Key, ConvertFromString(replace, propTypeName));
                            replacements = true;
                        }

                        var ptype = propTypeName;

                    });
            }

            await Task.WhenAll(valuesTask);

            return replacements;

        }

        private bool CheckEachField()
        {
            var replacements = false;
            foreach (var field in InputFields)
            {
                var name = field.FieldName;
                var info = field.FieldInfo;
                var type = field.FieldType;
                var typeName = field.FieldTypeName;
                var complete = field.ReplaceComplete;
                var values = field.FieldValues;
                if (values == null) continue;
                if (complete) continue;

                for (var i = 0; i < values.Count; i++)
                {
                    if (values[i].Value == null) continue;
                    var property = values[i].Key;
                    var propTypeName = values[i].Value.GetType().ToString();
                    var propValue = values[i].Value;

                    var regex = Regex.Match(propTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");
                    if (regex.Success && regex.Groups[5].Value.Contains("System.String"))
                    {
                        if (regex.Groups[1].Value == "System.Collections.ObjectModel.ObservableCollection")
                        {
                            var vals = (ObservableCollection<string>) propValue;
                            var valueSet = false;
                            var tokens = false;
                            Parallel.For(0, vals.Count, n =>
                            {
                                if (vals[n] == null) return;
                                var fname = $"{property}[{n}]";
                                var unique = CheckUniqueField(name);
                                if (unique != null && unique.VerifiedValues.ContainsKey(fname)) return;
                                var replace = unique != null ? ReplaceUnique(fname, vals[n], unique.IdentityType) : ReplaceTokens(fname, vals[n]);
                                if (!ContainsValidToken(replace) && replace != vals[n])
                                {
                                    vals[n] = replace;
                                    valueSet = true;
                                    replacements = true;
                                }
                                else
                                {
                                    if (ContainsValidToken(vals[n])) tokens = true;
                                    if (replace == vals[n]) return;
                                    vals[n] = replace;
                                    replacements = true;
                                }
                            });
                            if (valueSet)
                            {
                                field.FieldValues[i] = new KeyValuePair<string, object>(values[i].Key, vals);
                                info.SetValue(InputObject, vals);
                            }
                            field.ReplaceComplete = tokens == false;
                            continue;
                        }
                        else
                        {

                            dynamic vals = propValue;
                            var valueSet = false;
                            var tokens = false;
                            var tasks = new Task[vals.Count];
                            for (int n = 0; n < vals.Count; n++)
                            {
                                int task = n;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        if (vals[task] == null) return;
                                        var fname = $"{property}[{task}]";
                                        var unique = CheckUniqueField(name);
                                        if (unique != null && unique.VerifiedValues.ContainsKey(fname)) return;
                                        var replace = unique != null ? ReplaceUnique(fname, vals[task], unique.IdentityType) : ReplaceTokens(fname, vals[task]);
                                        if (!ContainsValidToken(replace) && replace != vals[task])
                                        {
                                            vals[task] = replace;
                                            valueSet = true;
                                            replacements = true;
                                            return;
                                        }
                                        if (ContainsValidToken(vals[task])) tokens = true;
                                        if ((string) replace == (string) vals[task]) return;
                                        vals[task] = replace;
                                        replacements = true;
                                    });
                            }
                            Task.WaitAll(tasks);
                            if (valueSet)
                            {
                                values[i] = new KeyValuePair<string, object>(values[i].Key, vals);
                                info.SetValue(InputObject, vals);
                            }
                            field.ReplaceComplete = tokens == false;
                            continue;
                        }
                    }

                    if (propTypeName == "System.String")
                    {
                        dynamic val = propValue;
                        var fname = $"{property}";
                        var unique = CheckUniqueField(name);
                        if (unique != null && unique.VerifiedValues.ContainsKey(name))
                        {
                            field.ReplaceComplete = true;
                            continue;
                        }
                        if (unique == null && !ContainsValidToken(val))
                        {
                            field.ReplaceComplete = true;
                            continue;
                        }
                        var replace = unique != null ? ReplaceUnique(fname, val, unique.IdentityType) : ReplaceTokens(fname, val);
                        if (!ContainsValidToken(replace) && (string) replace != (string) val)
                        {
                            val = replace;
                            values[i] = new KeyValuePair<string, object>(values[i].Key, replace);
                            info.SetValue(InputObject, val);
                            field.ReplaceComplete = true;
                            replacements = true;
                        }
                        else
                        {
                            if ((string) val != (string) replace)
                            {
                                val = replace;
                                replacements = true;
                            }
                            field.ReplaceComplete = ContainsValidToken(val) == false;
                        }
                    }
                    if (TemplateSettings.ReplaceStringFieldsOnly) continue;
                    if (propTypeName == "System.Boolean" || propTypeName == "System.Int32" || propTypeName == "System.Int64" || propTypeName == "System.Char")
                    {
                        var strValue = propValue.ToString();
                        var unique = CheckUniqueField(typeName);
                        if (unique != null && unique.VerifiedValues.ContainsKey(typeName))
                        {
                            field.ReplaceComplete = true;
                            continue;
                        }
                        if (unique == null && !ContainsValidToken(strValue))
                        {
                            field.ReplaceComplete = true;
                            continue;
                        }
                        var replace = unique != null ? ReplaceUnique(property, strValue, unique.IdentityType) : ReplaceTokens(property, strValue);
                        if (!ContainsValidToken(replace))
                        {
                            values[i] = new KeyValuePair<string, object>(values[i].Key, ConvertFromString(replace, propTypeName));
                            replacements = true;
                            info.SetValue(InputObject, replace);
                            field.ReplaceComplete = true;
                            continue;
                        }
                        if (replace == strValue) continue;
                        values[i] = new KeyValuePair<string, object>(values[i].Key, ConvertFromString(replace, propTypeName));
                        replacements = true;
                    }
                }
            }
            return replacements;
        }

        private List<KeyValuePair<string, object>> GetCollectionValues(string fieldName, object fieldValue)
        {
            var subObjects = new List<KeyValuePair<string, object>>();

            foreach (var property in fieldValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                var propValue = property.GetValue(fieldValue);
                if (propValue == null) continue;
                var propType = propValue.GetType().ToString();
                var regex = Regex.Match(propType, @"(.*)(`)(\d+)(\[)(.*)(\])");
                if (regex.Success)
                {
                    var baseType = regex.Groups[1].Value;
                    var subTypeCount = int.Parse(regex.Groups[3].Value);
                    var subTypes = regex.Groups[5].Value.Split(',');
                    var customObject = false;
                    foreach (var sub in subTypes)
                    {
                        if (_knownCollectionTypes.All(t => t != sub) && _knownStandardTypes.All(t => t != sub))
                        {
                            customObject = true;
                        }
                    }
                    if (customObject)
                    {
                        //subObjects = GetCollectionValues($"{fieldName}.{property.Name}", propValue);
                        if (baseType != "System.Collections.Generic.KeyValuePair")
                        {
                            dynamic val = propValue;
                            var subs = subObjects;
                            var tasks = new Task[val.Count];
                            for (var i = 0; i < val.Count; i++)
                            {
                                int task = i;
                                tasks[task] = Task.Factory.StartNew(
                                    () =>
                                    {
                                        var cvals = GetCollectionValues($"{fieldName}.{property.Name}[{task}]", val[task]);
                                        foreach (var cval in cvals)
                                        {
                                            subs.Add(new KeyValuePair<string, object>(cval.Key, cval.Value));
                                        }
                                    });
                            }
                            Task.WhenAll(tasks).Wait();
                        }
                    }
                    else
                    {
                        if (baseType == "System.Collections.Generic.KeyValuePair")
                        {
                            //dynamic val = propValue;
                            //for (var i = 0; i < val.Count; i++)
                            //{
                            //    var k = val[i].Key;
                            //    var v = val[i].Value;
                            //    var fname = $"{fieldName}.{property.Name}[{i}]";
                            //    var fkey = $"{fieldName}.{property.Name}[{i}].Key";
                            //    var fval = $"{fieldName}.{property.Name}[{i}].Value";
                            //    string fvReplace = ReplaceTokens(fval, v);
                            //    string fnReplace = ReplaceTokens(fname, $"{k}:{fvReplace}");
                            //    val[i].Value = ConvertFromString(fvReplace, subTypes[1]);
                            //}
                            //subObjects.Add(new KeyValuePair<string, object>($"{fieldName}.{property.Name}", val));
                        }
                        else
                        {

                            if (baseType == "System.Collections.Generic.List" && subTypes[0] == "System.String")
                            {
                                var val = (List<string>) propValue;
                                Parallel.For(0, val.Count, i =>
                                {
                                    if (val[i] == null) return;
                                    var fname = $"{fieldName}.{property.Name}[{i}]";
                                    var unique = CheckUniqueField(fname);
                                    var replace = unique != null
                                        ? ReplaceUnique(fname, val[i], unique.IdentityType)
                                        : ReplaceTokens(fname, val[i]);
                                    val[i] = ConvertFromString(replace, subTypes[0]);
                                });
                                subObjects.Add(new KeyValuePair<string, object>($"{fieldName}.{property.Name}", val));
                            }
                            else
                            {
                                dynamic val = propValue;
                                var tasks = new Task[val.Count];
                                for (int i = 0; i < val.Count; i++)
                                {
                                    int task = i;
                                    tasks[task] = Task.Factory.StartNew(
                                        () =>
                                        {
                                            if (val[task] == null) return;
                                            var fname = $"{fieldName}.{property.Name}[{task}]";
                                            var unique = CheckUniqueField(fname);
                                            var replace = unique != null ? ReplaceUnique(fname, val[task].ToString(), unique.IdentityType) : ReplaceTokens(fname, val[task]);
                                            val[task] = ConvertFromString(replace, subTypes[0]);
                                        });
                                }

                                Task.WaitAll(tasks);
                                subObjects.Add(new KeyValuePair<string, object>($"{fieldName}.{property.Name}", val));
                            }
                        }
                    }
                }
                else
                {
                    var fname = $"{fieldName}.{property.Name}";
                    var unique = CheckUniqueField(fname);
                    var replace = unique != null ? ReplaceUnique(fname, propValue.ToString(), unique.IdentityType) : ReplaceTokens(fname, propValue);
                    propValue = ConvertFromString(replace, propType);
                    subObjects.Add(new KeyValuePair<string, object>($"{fieldName}.{property.Name}", propValue));
                }
            }
            return subObjects;
        }

        private dynamic ReplaceDynamicValue(string fieldName, dynamic val)
        {
            var unique = CheckUniqueField(fieldName);
            return unique != null ? ReplaceUnique(fieldName, val.ToString(), unique.IdentityType) : ReplaceTokens(fieldName, val);
        }

        private void SetValues()
        {
            foreach (var field in InputFields)
            {
                var name = field.FieldName;
                var info = field.FieldInfo;
                var type = field.FieldType;
                var typeName = field.FieldTypeName;
                var tokens = field.ReplaceComplete == false;
                var values = field.FieldValues;


                foreach (var val in values)
                {
                    if (val.Value == null) continue;
                    var property = val.Key;
                    var valType = val.Value.GetType().ToString();
                    var propertyValue = val.Value;
                    //info.SetValue(InputObject, propertyValue);
                    Reflector.SetValue(InputObject, property, propertyValue);
                }
            }
        }

        private dynamic ConvertFromString(string replace, string type)
        {
            dynamic val = replace;
            if (type == "System.Boolean")
            {
                Boolean result;
                if (Boolean.TryParse(replace, out result))
                {
                    val = result;
                }
            }
            if (type == "System.Int32")
            {
                Int32 result;
                if (Int32.TryParse(replace, out result))
                {
                    val = result;
                }
            }
            if (type == "System.Int64")
            {
                Int64 result;
                if (Int64.TryParse(replace, out result))
                {
                    val = result;
                }
            }
            if (type == "System.Char")
            {
                Char result;
                if (Char.TryParse(replace, out result))
                {
                    val = result;
                }
            }
            if (type == "System.DateTime")
            {
                DateTime result;
                if (DateTime.TryParse(replace, out result))
                {
                    val = result;
                }
            }

            return val;
        }

        private void AddKnownBaseTypeToDictionary(string fieldName, object fieldValue)
        {
            var propType = fieldValue.GetType();
            var stop = "here";
            switch (propType.ToString())
            {
                case "System.Boolean":
                    {
                        var val = (bool) fieldValue;
                        AddTokenToReplacementDictionary(fieldName, val.ToString());
                    }
                    break;
                case "System.String":
                    {
                        var val = (string) fieldValue;
                        AddTokenToReplacementDictionary(fieldName, val);
                    }
                    break;
                case "System.Int32":
                    {
                        var val = Convert.ToInt32(fieldValue);
                        AddTokenToReplacementDictionary(fieldName, val.ToString());
                    }
                    break;
                case "System.Int64":
                    {
                        var val = Convert.ToInt64(fieldValue);
                        AddTokenToReplacementDictionary(fieldName, val.ToString());
                    }
                    break;
                case "System.String[]":
                    {
                        var val = (string[]) fieldValue;
                        for (var i = 0; i < val.Length; i++)
                        {
                            AddTokenToReplacementDictionary($"{fieldValue}{i}", val[i]);
                        }
                    }
                    break;
                case "System.Char[]":
                    {
                        var val = (char[]) fieldValue;
                        for (var i = 0; i < val.Length; i++)
                        {
                            AddTokenToReplacementDictionary($"{fieldValue}{i}", val[i].ToString());
                        }
                    }
                    break;
            }
        }

        private void AddTokenToReplacementDictionary(string tokenName, string tokenValue)
        {
            if (string.IsNullOrWhiteSpace(tokenValue)) return;

            var x = tokenValue.Length < 4 ? 4 : tokenValue.Length;

            if (UniqueFieldsDictionary.Any(t => Regex.Match(tokenName, $@"({t.Key})(\[\d+\])?", RegexOptions.IgnoreCase).Success))
            {
                if (tokenValue.Contains("#"))
                {
                    return;
                }
                var baseToken = tokenName;
                var baseTokenMatch = Regex.Match(tokenName, $@"(.*)(\[\d+\])");
                if (baseTokenMatch.Success)
                {
                    baseToken = baseTokenMatch.Groups[1].Value;
                    if (UniqueFieldsDictionary[baseToken].IdentityType == IdentityType.SamAccountName)
                    {
                        x = tokenValue.Length < 4 ? 4 : tokenValue.Length > 20 ? 20 : tokenValue.Length;
                    }
                }
            }


            ReplacementDictionary[$"{{{{{tokenName}}}}}"] = tokenValue; // {LastName.[0,3]}
            ReplacementDictionary[$"{{L{{{tokenName}}}}}"] = tokenValue.ToLower(); // {LastName.[0,3]}
            ReplacementDictionary[$"{{U{{{tokenName}}}}}"] = tokenValue.ToUpper(); // {LastName.[0,3]}


            for (var i = 0; i < x; i++)
            {
                var d = tokenValue.Length > i ? i + 1 : tokenValue.Length;
                ReplacementDictionary[$"{{{{[{i + 1}]{tokenName}}}}}"] = tokenValue.Substring(0, d);
                ReplacementDictionary[$"{{L{{[{i + 1}]{tokenName}}}}}"] = tokenValue.Substring(0, d).ToLower();
                ReplacementDictionary[$"{{U{{[{i + 1}]{tokenName}}}}}"] = tokenValue.Substring(0, d).ToUpper();
            }
            for (var i = 0; i < x; i++)
            {
                var d = tokenValue.Length > i ? i + 1 : tokenValue.Length;
                var f = tokenValue.Length - i - 1 < 0 ? 0 : tokenValue.Length - i - 1;
                ReplacementDictionary[$"{{{{{tokenName}[{i + 1}]}}}}"] = tokenValue.Substring(f, d);
                ReplacementDictionary[$"{{L{{{tokenName}[{i + 1}]}}}}"] = tokenValue.Substring(f, d).ToLower();
                ReplacementDictionary[$"{{U{{{tokenName}[{i + 1}]}}}}"] = tokenValue.Substring(f, d).ToUpper();
            }


            var splitWords = tokenValue.Split(' ');
            if (splitWords.Length > 1)
            {
                for (var i = 1; i <= splitWords.Length; i++)
                {
                    if (splitWords.Length >= i)
                    {
                        ReplacementDictionary[$"{{{{{tokenName}{{{i}}}}}}}"] = splitWords[i - 1];
                        ReplacementDictionary[$"{{L{{{tokenName}{{{i}}}}}}}"] = splitWords[i - 1].ToLower();
                        ReplacementDictionary[$"{{U{{{tokenName}{{{i}}}}}}}"] = splitWords[i - 1].ToUpper();
                    }
                }
            }

            //splitWords = tokenValue.Split('@');
            //if (splitWords.Length > 1)
            //{
            //    for (var i = 1; i <= splitWords.Length; i++)
            //    {
            //        if (splitWords.Length >= i)
            //        {
            //            ReplacementDictionary[$"{{{{{tokenName}{{@}}{{{i}}}}}}}"] = splitWords[i - 1];
            //            ReplacementDictionary[$"{{L{{{tokenName}{{@}}{{{i}}}}}}}"] = splitWords[i - 1].ToLower();
            //            ReplacementDictionary[$"{{U{{{tokenName}{{@}}{{{i}}}}}}}"] = splitWords[i - 1].ToUpper();
            //        }
            //    }
            //}

            //splitWords = tokenValue.Split('_');
            //if (splitWords.Length > 1)
            //{
            //    for (var i = 1; i <= splitWords.Length; i++)
            //    {
            //        if (splitWords.Length >= i)
            //        {
            //            ReplacementDictionary[$"{{{{{tokenName}{{_}}{{{i}}}}}}}"] = splitWords[i - 1];
            //            ReplacementDictionary[$"{{L{{{tokenName}{{_}}{{{i}}}}}}}"] = splitWords[i - 1].ToLower();
            //            ReplacementDictionary[$"{{U{{{tokenName}{{_}}{{{i}}}}}}}"] = splitWords[i - 1].ToUpper();
            //        }
            //    }
            //}

            //splitWords = tokenValue.Split('.');
            //if (splitWords.Length > 1)
            //{
            //    for (var i = 1; i <= splitWords.Length; i++)
            //    {
            //        if (splitWords.Length >= i)
            //        {
            //            ReplacementDictionary[$"{{{{{tokenName}{{.}}{{{i}}}}}}}"] = splitWords[i - 1];
            //            ReplacementDictionary[$"{{L{{{tokenName}{{.}}{{{i}}}}}}}"] = splitWords[i - 1].ToLower();
            //            ReplacementDictionary[$"{{U{{{tokenName}{{.}}{{{i}}}}}}}"] = splitWords[i - 1].ToUpper();
            //        }
            //    }
            //}
        }

        private string ReplaceUnique(string fieldName, string fieldValue, IdentityType type)
        {
            if (fieldValue == null) return null;

            var uniqueDic = UniqueFieldsDictionary.FirstOrDefault(t => Regex.Match(fieldName, $@"({t.Key})(\[\d+\])?").Success);

            if (uniqueDic.Key == null) return fieldValue;

            if (fieldValue.Contains('|'))
            {
                var sp = fieldValue.Split('|');
                var x = 0;
                var id = TemplateSettings.TrimWhitespace ? sp[x].Trim() : sp[x];
                if (!UniqueReplacementRequired(id, type)) return id;
                var newValue = ReplaceTokens(fieldName, id, true);
                if (ContainsValidToken(newValue)) return fieldValue;
                newValue = ReplaceNumber(fieldName, newValue, type);
                var exists = UserExists(newValue, type);
                while (exists && x < sp.Length)
                {
                    x++;
                    if (x < sp.Length)
                    {
                        id = TemplateSettings.TrimWhitespace ? sp[x].Trim() : sp[x];
                        newValue = ReplaceTokens(fieldName, id, true);
                        newValue = ReplaceNumber(fieldName, newValue, type);
                        exists = UserExists(newValue, type);
                    }
                }

                if (exists)
                {
                    ErrorDictionary[fieldName] =
                        $"All options already exist for field {fieldName} ({fieldValue}). Last Attempt: {newValue}. Specify ## to append number or use alternative format: \"original | alternative1 | alternavtive2\"";
                    uniqueDic.Value.VerifiedValues[fieldName] = newValue;
                    uniqueDic.Value.AllValuesExist = true;
                    return fieldValue;
                }

                if (ContainsValidToken(newValue)) return fieldValue;
                uniqueDic.Value.VerifiedValues[fieldName] = newValue;
                AddTokenToReplacementDictionary(fieldName, newValue);
                return newValue;
            }
            else
            {
                var newValue = TemplateSettings.TrimWhitespace ? fieldValue.Trim() : fieldValue;
                if (UniqueReplacementRequired(newValue, type))
                {
                    newValue = ReplaceTokens(fieldName, newValue);
                    if (ContainsValidToken(newValue)) return newValue;
                    newValue = ReplaceNumber(fieldName, newValue, type);
                    if (UserExists(newValue, type))
                    {
                        ErrorDictionary[fieldName] =
                            $"All options already exist for field {fieldName} ({fieldValue}). Last Attempt: {newValue}";
                        uniqueDic.Value.VerifiedValues[fieldName] = newValue;
                        uniqueDic.Value.AllValuesExist = true;
                        return fieldValue;
                    }
                }

                uniqueDic.Value.VerifiedValues[fieldName] = newValue;
                AddTokenToReplacementDictionary(fieldName, newValue);
                return newValue;
            }
        }

        private string ReplaceNumber(string fieldName, string fieldValue, IdentityType type)
        {
            string character = null;
            var noeqone = true;
            var firstAvailble = false;
            var checkExistingUpTo = 3;
            var requiredOnly = false;
            foreach (var numberReplacement in NumberReplacements)
            {
                if (fieldValue.Contains(numberReplacement.Character))
                {
                    character = numberReplacement.Character;
                    noeqone = numberReplacement.NoNumberIsOne;
                    firstAvailble = numberReplacement.FirstAvailableNumber;
                    checkExistingUpTo = numberReplacement.CheckForExistingUsersUpTo;
                    requiredOnly = numberReplacement.OnlyAppendNumberIfRequired;
                }
            }

            if (!UniqueFieldsDictionary.Any(t => Regex.Match(fieldName, $@"({t.Key})(\[\d+\])?", RegexOptions.IgnoreCase).Success))
            {
                return fieldValue;
            }
            if (ContainsValidToken(fieldValue))
            {
                return fieldValue;
            }
            if (!Regex.Match(fieldValue, "\\d+").Success && character == null)
            {
                return fieldValue;
            }

            var newFieldValue = fieldValue;
            if (character != null)
            {
                newFieldValue = newFieldValue.Replace(character, "0");
            }
            else
            {
                if (!UserExists(fieldValue, type))
                {
                    return fieldValue;
                }
            }

            var exNum = -1;
            var withoutNumbers = Regex.Replace(newFieldValue, "\\d+", string.Empty);
            var noNumberExists = UserExists(withoutNumbers, type);

            if (noNumberExists)
            {
                exNum = noeqone ? 1 : 0;
            }
            else
            {
                exNum = 0;
            }

            Int32 n;
            Int32.TryParse(Regex.Match(newFieldValue, "\\d+").Value, out n);
            if (n != 0)
            {
                // turn value into all zeros
                newFieldValue = Regex.Replace(newFieldValue, "\\d+",
                    m => (int.Parse(m.Value) - n).ToString(new string('0', m.Value.Length)));
            }


            if (requiredOnly)
            {
                if (firstAvailble)
                {
                    if (!noNumberExists)
                    {
                        // Number not required and no number does not exist, so use value without a number.
                        return withoutNumbers;
                    }
                    // Number not required and no number exists, so use next value starting at 1 and 2 (if noeqone)
                    newFieldValue = Regex.Replace(newFieldValue, "\\d+", m => (int.Parse(m.Value) + 1 + exNum).ToString(new string('0', m.Value.Length)));
                    while (UserExists(newFieldValue, type))
                    {
                        newFieldValue = Regex.Replace(newFieldValue, "\\d+", m => (int.Parse(m.Value) + 1).ToString(new string('0', m.Value.Length)));
                    }
                    return newFieldValue;
                }
                // check up to required threshold. 
                var testValue = newFieldValue;
                var tasks = new Task[checkExistingUpTo];
                for (int i = 0; i < checkExistingUpTo; i++)
                {
                    int task = i + 1;
                    var curValue = newFieldValue;
                    tasks[i] = Task.Factory.StartNew(
                        () =>
                        {
                            // starting with 1, check 1 through existing threshold setting to see if it exists
                            testValue = Regex.Replace(curValue, "\\d+", m => (int.Parse(m.Value) + task).ToString(new string('0', m.Value.Length)));
                            if (UserExists(testValue, type))
                            {
                                // capture the highest existing integer
                                if (task > exNum)
                                {
                                    exNum = task;
                                }
                            }
                        });
                }

                Task.WhenAll(tasks).Wait();
                // If none exist, return nonumber. If one exists, return highest found plus 1 (next available).
                if (exNum > 0 || noNumberExists)
                {
                    return Regex.Replace(newFieldValue, "\\d+",
                        m => (int.Parse(m.Value) + exNum + 1).ToString(new string('0', m.Value.Length)));
                }
                else
                {
                    return withoutNumbers;
                }
            }
            else
            {

                if (firstAvailble)
                {
                    newFieldValue = Regex.Replace(newFieldValue, "\\d+", m => (int.Parse(m.Value) + 1 + exNum).ToString(new string('0', m.Value.Length)));
                    while (UserExists(newFieldValue, type))
                    {
                        newFieldValue = Regex.Replace(newFieldValue, "\\d+", m => (int.Parse(m.Value) + 1).ToString(new string('0', m.Value.Length)));
                    }
                    return newFieldValue;
                }

                // Need to check if any number exists up to the set threshold. 
                var testValue = newFieldValue;
                var tasks = new Task[checkExistingUpTo];
                for (int i = 0; i < checkExistingUpTo; i++)
                {
                    int task = i + 1;
                    var curValue = newFieldValue;
                    tasks[i] = Task.Factory.StartNew(
                        () =>
                        {
                            // starting with 1, check 1 through existing threshold setting to see if it exists
                            testValue = Regex.Replace(curValue, "\\d+",
                                    m => (int.Parse(m.Value) + task).ToString(new string('0', m.Value.Length)));
                            if (UserExists(testValue, type))
                            {
                                // capture the highest existing integer
                                if (task > exNum)
                                {
                                    exNum = task;
                                }
                            }
                        });
                }
                Task.WhenAll(tasks).Wait();

                return Regex.Replace(newFieldValue, "\\d+",
                            m => (int.Parse(m.Value) + exNum + 1).ToString(new string('0', m.Value.Length)));

            }
        }

        private string ReplaceTokens(string fieldName, object fieldValue, bool noDictionary = false)
        {
            var newValue = fieldValue.ToString();
            if (string.IsNullOrWhiteSpace(newValue)) return null;

            newValue = Replace(newValue);

            if (noDictionary) return newValue;


            if (!ContainsValidToken(newValue))
            {
                AddKnownBaseTypeToDictionary(fieldName, newValue);
            }

            return newValue;
        }

        private List<DeconstructedField> AddedFields { get; set; } = new List<DeconstructedField>();
        public List<KeyValuePair<string,DateTime>> DateTimeList { get; set; } = new List<KeyValuePair<string, DateTime>>();
        private string Replace(string original)
        {
            var newValue = original;
            var spMatch = false;

            foreach (KeyValuePair<string, DateTime> time in DateTimeList)
            {

                var dtr = Regex.Matches(newValue, $@"({{[L,U]?{{)({time.Key}[:,-,\s]?)(.*)(}}}})", RegexOptions.IgnoreCase);

                foreach (Match match in dtr)
                {
                    var pattern = match.Groups[3].Value;
                    var formattedDate = time.Value.ToString(pattern);
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
            }
            foreach (var field in InputFields)
            {
                var regex = Regex.Match(newValue, $@"({{[L,U]?{{)(\[\d+\])?({field.FieldName})(\[\d+\])?({{.}})({{\d}})(\[\d+\])?(}}}})");
                if (regex.Success && !ReplacementDictionary.ContainsKey(regex.Groups[0].Value))
                {
                    var splitRegex = Regex.Match(regex.Groups[5].Value, $@"({{)(.)(}})");
                    var splitIndexRegex = Regex.Match(regex.Groups[6].Value, $@"({{)(\d+)(}})");

                    if (!splitRegex.Success) continue;
                    var splitBy = splitRegex.Groups[2].Value;
                    Int32 spIndex;
                    if (!Int32.TryParse(splitIndexRegex.Groups[2].Value, out spIndex)) continue;

                    var upper = false;
                    var lower = false;
                    var capitalCheck = Regex.Match(regex.Groups[1].Value, $@"({{)([L,U])({{)");
                    if (capitalCheck.Success)
                    {
                        if (capitalCheck.Groups[2].Value == "L") lower = true;
                        if (capitalCheck.Groups[2].Value == "U") upper = true;
                    }
                    var fSubstringCheck = Regex.Match(regex.Groups[2].Value, $@"(\[\d+\])");
                    var fSubstring = false;
                    Int32 fSub = 0;
                    if (fSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(regex.Groups[2].Value, "\\d+").Value, out fSub))
                        {
                            fSubstring = true;
                        }
                    }
                    var bSubstringCheck = Regex.Match(regex.Groups[7].Value, $@"(\[\d+\])");
                    var bSubstring = false;
                    Int32 bSub = 0;
                    if (!fSubstring && bSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(regex.Groups[7].Value, "\\d+").Value, out bSub))
                        {
                            bSubstring = true;
                        }
                    }
                    var fieldValue = field.FieldValues[0].Value;
                    var fieldTypeName = fieldValue.GetType().ToString();
                    var multiValueCheck = Regex.Match(fieldTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");

                    if (multiValueCheck.Success && regex.Groups[4].Value != "")
                    {
                        // Multi Value Field
                        Int32 vIndex;
                        if (!Int32.TryParse(Regex.Match(regex.Groups[4].Value, "\\d+").Value, out vIndex)) continue;
                        dynamic val = fieldValue;
                        var strValue = val[vIndex].ToString();
                        if (!strValue.Contains(splitBy)) continue;
                        if (ReplacementRequired($"{field.FieldName}{vIndex}", strValue)) continue;
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

                        ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                    }
                    else
                    {
                        // Single Value Field
                        if (fieldTypeName == "System.String")
                        {
                            var strValue = fieldValue.ToString();
                            if (ReplacementRequired(field.FieldName, strValue)) continue;
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

                            ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                        }
                        if (TemplateSettings.ReplaceStringFieldsOnly) continue;
                        if (fieldTypeName == "System.Boolean" || fieldTypeName == "System.Int32" || fieldTypeName == "System.Int64")
                        {
                            var strValue = fieldValue.ToString();
                            if (ReplacementRequired(field.FieldName, strValue)) continue;
                            if (!strValue.Contains(splitBy)) continue;
                            var sp = strValue.Split(new[] { splitBy }, StringSplitOptions.None);
                            if (sp.Length <= spIndex)
                            {
                                spIndex = sp.Length - 1;
                            }
                            if (upper) sp[spIndex] = sp[spIndex].ToUpper();
                            if (lower) sp[spIndex] = sp[spIndex].ToLower();
                            if (fSubstring) sp[spIndex] = sp[spIndex].Substring(0, fSub);
                            if (bSubstring) sp[spIndex] = sp[spIndex].Substring(sp[spIndex].Length - bSub, sp[spIndex].Length - 1);

                            ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                        }
                    }
                }
            }

            

            foreach (var field in AddedFields)
            {
                var regex = Regex.Match(newValue, $@"({{[L,U]?{{)(\[\d+\])?({field.FieldName})(\[\d+\])?({{.}})({{\d}})(\[\d+\])?(}}}})");
                if (regex.Success && !ReplacementDictionary.ContainsKey(regex.Groups[0].Value))
                {
                    var splitRegex = Regex.Match(regex.Groups[5].Value, $@"({{)(.)(}})");
                    var splitIndexRegex = Regex.Match(regex.Groups[6].Value, $@"({{)(\d+)(}})");

                    if (!splitRegex.Success) continue;
                    var splitBy = splitRegex.Groups[2].Value;
                    Int32 spIndex;
                    if (!Int32.TryParse(splitIndexRegex.Groups[2].Value, out spIndex)) continue;

                    var upper = false;
                    var lower = false;
                    var capitalCheck = Regex.Match(regex.Groups[1].Value, $@"({{)([L,U])({{)");
                    if (capitalCheck.Success)
                    {
                        if (capitalCheck.Groups[2].Value == "L") lower = true;
                        if (capitalCheck.Groups[2].Value == "U") upper = true;
                    }
                    var fSubstringCheck = Regex.Match(regex.Groups[2].Value, $@"(\[\d+\])");
                    var fSubstring = false;
                    Int32 fSub = 0;
                    if (fSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(regex.Groups[2].Value, "\\d+").Value, out fSub))
                        {
                            fSubstring = true;
                        }
                    }
                    var bSubstringCheck = Regex.Match(regex.Groups[7].Value, $@"(\[\d+\])");
                    var bSubstring = false;
                    Int32 bSub = 0;
                    if (!fSubstring && bSubstringCheck.Success)
                    {
                        if (Int32.TryParse(Regex.Match(regex.Groups[7].Value, "\\d+").Value, out bSub))
                        {
                            bSubstring = true;
                        }
                    }
                    var fieldValue = field.FieldValues[0].Value;
                    var fieldTypeName = fieldValue.GetType().ToString();
                    var multiValueCheck = Regex.Match(fieldTypeName, @"(.*)(`)(\d+)(\[)(.*)(\])");

                    if (multiValueCheck.Success && regex.Groups[4].Value != "")
                    {
                        // Multi Value Field
                        Int32 vIndex;
                        if (!Int32.TryParse(Regex.Match(regex.Groups[4].Value, "\\d+").Value, out vIndex)) continue;
                        dynamic val = fieldValue;
                        var strValue = val[vIndex].ToString();
                        if (!strValue.Contains(splitBy)) continue;
                        if (ReplacementRequired($"{field.FieldName}{vIndex}", strValue)) continue;
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

                        ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                    }
                    else
                    {
                        // Single Value Field
                        if (fieldTypeName == "System.String")
                        {
                            var strValue = fieldValue.ToString();
                            if (ReplacementRequired(field.FieldName, strValue)) continue;
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

                            ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                        }
                        if (TemplateSettings.ReplaceStringFieldsOnly) continue;
                        if (fieldTypeName == "System.Boolean" || fieldTypeName == "System.Int32" || fieldTypeName == "System.Int64")
                        {
                            var strValue = fieldValue.ToString();
                            if (ReplacementRequired(field.FieldName, strValue)) continue;
                            if (!strValue.Contains(splitBy)) continue;
                            var sp = strValue.Split(new[] { splitBy }, StringSplitOptions.None);
                            if (sp.Length <= spIndex)
                            {
                                spIndex = sp.Length - 1;
                            }
                            if (upper) sp[spIndex] = sp[spIndex].ToUpper();
                            if (lower) sp[spIndex] = sp[spIndex].ToLower();
                            if (fSubstring) sp[spIndex] = sp[spIndex].Substring(0, fSub);
                            if (bSubstring) sp[spIndex] = sp[spIndex].Substring(sp[spIndex].Length - bSub, sp[spIndex].Length - 1);

                            ReplacementDictionary[regex.Groups[0].Value] = sp[spIndex];
                        }
                    }
                }
            }

            foreach (var item in ReplacementDictionary)
            {
                if (item.Value.Contains("|")) continue;
                newValue = TemplateSettings.TrimWhitespace ? newValue.Replace(item.Key, item.Value).Trim() : newValue.Replace(item.Key, item.Value);
            }
            
            return newValue;
        }

        #endregion

        #region Private Testing Methods
        private UniqueField CheckUniqueField(string fieldName)
        {
            return UniqueFieldsDictionary.ContainsKey(fieldName) ? UniqueFieldsDictionary[fieldName] : null;
        }
        private List<MatchCollection> ExtractValidToken(string value)
        {
            return InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Select(t => Regex.Matches(value, $"({{[L,U]?{{)({t.Name})(}}}})")).ToList();
        }

        private List<string> ExtractValidTokenProperties(string value)
        {
            return InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Select(property => Regex.Match(value, $"({{[L,U]?{{)({property.Name})(}}}})")).Select(match => match.Groups[2].Value).ToList();
        }

        private List<string> ExtractAllTokenProperties(string value)
        {
            var matches = Regex.Matches(value, $"({{[L,U]?{{)(.*)(}}}})");
            return (from Match match in matches select match.Groups[2].Value).ToList();
        }

        private MatchCollection ExtractTokens(string value)
        {
            return Regex.Matches(value, $"({{[L,U]?{{)(.*)(}}}})");
        }

        private bool ContainsValidToken(string value)
        {
            if (value == null) return false;

            if (InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Any(t => Regex.Match(value, $@"({{[L,U]?{{)(\[\d+\])?({t.Name})(\[\d+\])?(\..*)?(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})").Success))
            {
                return true;
            }

            if (!ContainsTokenSyntax(value)) return false;
            var token = ExtractTokens(value);
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
        private bool ContainsFieldNames(string value)
        {
            return value != null && InputType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Any(uItem => value.Contains(uItem.Name));
        }
        private bool ContainsTokenSyntax(string value)
        {
            return value != null && Regex.Match(value, @"({[L,U]?{)(.*)(}})").Success;
        }

        private bool ReplacementRequired(string fieldName, string fieldValue)
        {

            if (ContainsValidToken(fieldValue)) return true;

            var uniqueDic = UniqueFieldsDictionary.FirstOrDefault(t => Regex.Match(fieldName, $@"({t.Key})(\[\d+\])?").Success);
            

            var unique = uniqueDic.Key != null;

            if (unique && UniqueReplacementRequired(fieldValue, uniqueDic.Value.IdentityType)) return true;

            if (unique && fieldValue.Contains("#")) return true;

            return false;
        }

        private bool UniqueReplacementRequired(string identity, IdentityType type)
        {
            if (ContainsValidToken(identity)) return true;


            foreach (var numberReplacement in NumberReplacements)
            {
                if (identity.Contains(numberReplacement.Character))
                {
                    return true;
                }
            }


            var digitString = Regex.Match(identity, "\\d+").Value;
            Int32 digits;
            if (Int32.TryParse(digitString, out digits))
            {
                if (digits == 0) return true;
            }

            var existing = AdObjectCheck.CheckObject(identity, type);

            if (existing != null)
            {
                return (bool) existing;
            }

            return UserExists(identity, type);
        }

        private bool UserExists(string identity, IdentityType type)
        {
            var existing = AdObjectCheck.CheckObject(identity, type);

            if (existing != null)
            {
                return (bool) existing;
            }

            if (string.IsNullOrWhiteSpace(TemplateSettings.DomainName))
            {
                TemplateSettings.DomainName = CurrentUser.User.UserDomain;
            }

            if (type == IdentityType.Sid && identity.Contains("@"))
            {
                var adEntry = new DirectoryEntry("LDAP://" + TemplateSettings.DomainName);
                DirectorySearcher adSearcher = new DirectorySearcher(adEntry);
                adSearcher.Filter = ($"(|(proxyAddresses=*smtp:{identity}*)(mail={identity}))");
                adSearcher.ReferralChasing = ReferralChasingOption.All;
                SearchResultCollection results = adSearcher.FindAll();
                if (results.Count > 0)
                {
                    AdObjectCheck.AddExisting(identity, type);
                    return true;
                }
                AdObjectCheck.AddOpen(identity, type);
                return false;
            }
            if (type == IdentityType.Sid && !identity.Contains("@"))
            {
                if (!TemplateSettings.RequireUniqueAlias) return false;
                var adEntry = new DirectoryEntry("LDAP://" + TemplateSettings.DomainName);
                DirectorySearcher adSearcher = new DirectorySearcher(adEntry);
                adSearcher.Filter = ("mailNickname=" + identity);
                SearchResultCollection results = adSearcher.FindAll();
                if (results.Count > 0)
                {
                    AdObjectCheck.AddExisting(identity, type);
                    return true;
                }
                AdObjectCheck.AddOpen(identity, type);
                return false;
            }

            using (var domainContext = new PrincipalContext(ContextType.Domain, TemplateSettings.DomainName))
            {
                using (var foundUser = UserPrincipal.FindByIdentity(domainContext, type, identity))
                {
                    if (foundUser == null)
                    {
                        AdObjectCheck.AddOpen(identity, type);
                        return false;
                    }
                    AdObjectCheck.AddExisting(identity, type);
                    return true;
                }
            }
        }

        #endregion

    }

    public class AdObjectTypes
    {
        public List<IdentityType> IdentityTypes { get; set; }
        public AdObjectTypes()
        {
            IdentityTypes = new List<IdentityType>();
        }

        public AdObjectTypes(IdentityType type)
        {
            IdentityTypes = new List<IdentityType>()
            {
                type,
            };
        }

        public void Add(IdentityType type)
        {
            if (!IdentityTypes.Contains(type))
            {
                IdentityTypes.Add(type);
            }
        }

    }

    public class AdObjectCheck
    {
        public Dictionary<string, AdObjectTypes> ExistingObjects { get; set; }
        public Dictionary<string, AdObjectTypes> OpenObjects { get; set; }
        public AdObjectCheck()
        {
            ExistingObjects = new Dictionary<string, AdObjectTypes>();
            OpenObjects = new Dictionary<string, AdObjectTypes>();
        }

        public bool? CheckObject(string identity, IdentityType type)
        {
            if (ExistingObjects.ContainsKey(identity))
            {
                if (ExistingObjects[identity].IdentityTypes.Contains(type))
                {
                    return true;
                }
            }

            if (OpenObjects.ContainsKey(identity))
            {
                if (OpenObjects[identity].IdentityTypes.Contains(type))
                {
                    return false;
                }
            }

            return null;
        }

        public void AddExisting(string identity, IdentityType type)
        {
            if (ExistingObjects.ContainsKey(identity))
            {
                if (!ExistingObjects[identity].IdentityTypes.Contains(type))
                {
                    ExistingObjects[identity].IdentityTypes.Add(type);
                }
            }
            else
            {
                ExistingObjects[identity] = new AdObjectTypes(type);
            }
        }

        public void AddOpen(string identity, IdentityType type)
        {
            if (OpenObjects.ContainsKey(identity))
            {
                if (!OpenObjects[identity].IdentityTypes.Contains(type))
                {
                    OpenObjects[identity].IdentityTypes.Add(type);
                }
            }
            else
            {
                OpenObjects[identity] = new AdObjectTypes(type);
            }
        }
    }

    public class UniqueValue
    {

        #region Constructors & Initialization
        public UniqueValue()
        {
            Values = new List<string>();
        }

        #endregion
        public List<string> Values { get; set; }

    }

    public class UniqueField
    {
        public UniqueField(IdentityType identityType, string fieldName)
        {
            FieldName = fieldName;
            IdentityType = identityType;
            VerifiedValues = new ConcurrentDictionary<string, string>();
            AllValuesExist = false;
            //FieldValues = new ConcurrentDictionary<int, UniqueValue> { [0] = new UniqueValue() };
        }

        public UniqueField(IdentityType identityType, string fieldName, FieldInfo fieldInfo)
        {
            FieldName = fieldName;
            IdentityType = identityType;
            VerifiedValues = new ConcurrentDictionary<string, string>();
            AllValuesExist = false;
            FieldInfo = fieldInfo;
            //FieldValues = new ConcurrentDictionary<int, UniqueValue> { [0] = new UniqueValue() };
        }

        public IdentityType IdentityType { get; set; }
        public string FieldType { get; set; }
        public string FieldName { get; set; }
        public FieldInfo FieldInfo { get; set; }
        public bool AllValuesExist { get; set; }
        //public ConcurrentDictionary<int, UniqueValue> FieldValues { get; set; }
        public ConcurrentDictionary<string, string> VerifiedValues { get; set; }


    }

    public class DeconstructedField
    {
        public FieldInfo FieldInfo { get; set; }
        public Type FieldType { get; set; }
        public string FieldName { get; set; }
        public string FieldTypeName { get; set; }
        public bool ReplaceComplete { get; set; }
        public List<KeyValuePair<string, object>> FieldValues { get; set; }
    }
}