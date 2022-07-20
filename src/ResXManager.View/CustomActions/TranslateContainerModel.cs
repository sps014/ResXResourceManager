using ResXManager.Model;
using System.Collections.Generic;

namespace ResX.Scripting
{
    /// <summary>
    /// Stores a row of Language Resource
    /// </summary>
    public class TranslateContainerModel
    {
        public string this[string lang]
        {
            get => CultureValues[lang];
            set => CultureValues.Add(lang, value);
        }
        //map of lang,value
        public Dictionary<string, string> CultureValues { get; } = new Dictionary<string, string>();
        public string Key { get; set; }
        public string UniqueName { get; set; }
        public string ProjectName { get; set; }
        public ResourceTableEntry Entry { get; set; }
    }
}