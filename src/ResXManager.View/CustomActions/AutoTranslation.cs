using ResXManager.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResX.Scripting
{
    public static class AutoTranslation
    {

        public static void Start(ResourceManager manager)
        {
 
            //get all resource representation
            var entries = manager.TableEntries.GroupBy(x => x.Container);
            int count = 0;
            //map of neutralLang, set of rows with same neutral value
            var cached_values = new Dictionary<string, HashSet<TranslateContainerModel>>();
            foreach (var gp in entries)
            {
                foreach (var resourceEntry in gp)
                {
                    //get key with all its text and lang
                    var translated = new TranslateContainerModel();
                    //our key is neutral values
                    translated.Key = resourceEntry.Values.GetValue(resourceEntry.NeutralLanguage.Culture);
                    foreach (var lang in resourceEntry.Languages)
                    {
                        var langStr = lang.Culture == null ? string.Empty : lang.Culture.Name;// handle neural case with no culture
                        var val = resourceEntry.Values.GetValue(lang.Culture); 
                        translated[langStr] = val;//add culture info and value in row 
                    }

                    ///handle null case where we have our first entry
                    if (!cached_values.ContainsKey(translated.Key))
                        cached_values.Add(translated.Key, new HashSet<TranslateContainerModel>());

                    //for given neutral value add row to hash set
                    cached_values[translated.Key].Add(translated);

                }
            }


            //create snap
            File.WriteAllText("backup.snapshot",manager.CreateSnapshot());

            foreach (var gp in entries)
            {
                foreach (var resourceEntry in gp)
                {
                    //neutral lang culture
                    var neutralLang = resourceEntry.NeutralLanguage.Culture;
                    //neutral culture value
                    var neutralVal = resourceEntry.Values.GetValue(neutralLang);
                    foreach (var lang in resourceEntry.Languages)
                    {
                        //skip neural lang
                        if (lang == neutralLang)
                            continue;

                        //get current value of resource
                        var val = resourceEntry.Values.GetValue(lang.Culture);

                        //if translation in current cell is not null , do not touch it
                        if (!string.IsNullOrEmpty(val))
                            continue;

                        //if we dont have neutral translation skip
                        if (!cached_values.ContainsKey(neutralVal))
                            continue;
                        var curLang = lang.Culture == null ? string.Empty : lang.Culture.Name;
                        //load value from other cacheed resources with same neutral value whose same lang translation is not null
                        var value = cached_values[neutralVal]
                            .FirstOrDefault(x =>
                            x.CultureValues.
                            ContainsKey(curLang) 
                            && !string.IsNullOrEmpty(x.CultureValues[curLang]));

                        //if we dont have such translation then continue
                        if (value == null)
                            continue;

                        //update the resouece file
                        resourceEntry.Values.SetValue(lang.Culture, value[lang.Culture.Name]);
                        resourceEntry.Comments.SetValue(lang.Culture, "@autotranslated");

                        count++;
                    }
                }
            }

            //save changes in resource
            manager.Save();
        }
    }
}
