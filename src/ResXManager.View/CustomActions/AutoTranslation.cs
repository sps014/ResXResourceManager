using ResXManager.Model;
using ResXManager.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ResX.Scripting
{
    public static class AutoTranslation
    {
        public static async void Start(ResourceManager manager)
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
                    var translated = new TranslateContainerModel
                    {
                        //our key is neutral values
                        Key = resourceEntry.Values.GetValue(resourceEntry.NeutralLanguage.Culture),
                        ProjectName = resourceEntry.Container.ProjectName,
                        UniqueName = resourceEntry.Container.UniqueName,
                        Entry = resourceEntry
                    };
                    foreach (var lang in resourceEntry.Languages)
                    {
                        var langStr = lang.Culture == null ? string.Empty : lang.Culture.Name;// handle neural case with no culture
                        var val = resourceEntry.Values.GetValue(lang.Culture); 
                        translated[langStr] = val;//add culture info and value in row 
                    }

                    ///handle null case where we have our first entry
                    if ( translated.Key!=null && !cached_values.ContainsKey(translated.Key))
                        cached_values.Add(translated.Key, new HashSet<TranslateContainerModel>());

                    if(translated.Key!=null)
                    //for given neutral value add row to hash set
                        cached_values[translated.Key].Add(translated);


                    OnProgress?.Invoke(new ProgressEventArg
                    {
                        EventType = EventType.CacheBuildUp,
                    });
                }
            }

            if (!Directory.Exists("C:\\resxbackup"))
                Directory.CreateDirectory("C:\\resxbackup");
            //create snap
            File.WriteAllText($"C:\\resxbackup\\backup_{DateTime.UtcNow.Ticks}.snapshot",manager.CreateSnapshot());

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
                        ////delete auto trans values
                        //if (resourceEntry.Comments.GetValue(lang.Culture) == "@autotranslated")
                        //{
                        //    resourceEntry.SetCommentText(lang.Culture, string.Empty);
                        //    resourceEntry.Values.SetValue(lang.Culture, string.Empty);
                        //}


                        //get current value of resource
                        var val = resourceEntry.Values.GetValue(lang.Culture);

                        //if translation in current cell is not null , do not touch it
                        if (!string.IsNullOrEmpty(val))
                            continue;

                        //if we dont have neutral translation skip
                        if (neutralVal is null)
                            continue;
                        if (!cached_values.ContainsKey(neutralVal))
                            continue;
                        var curLang = lang.Culture == null ? string.Empty : lang.Culture.Name;

                        var values = cached_values[neutralVal]
                            .Where(x => x.CultureValues.ContainsKey(curLang)
                            && !string.IsNullOrEmpty(x.CultureValues[curLang])).ToImmutableArray();

                        if (!values.Any())
                            continue;

                        var res = await OnTranslationAction?.Invoke(new RequireActionEventArg
                        {
                            Key = resourceEntry.Key,
                            ProjectName = resourceEntry.Container.ProjectName,
                            UniqueName = resourceEntry.Container.UniqueName,
                            Values = values,
                            Culture = curLang,
                            NeutralText = neutralVal
                        });
                        if (!res.Merge)
                            continue;

                        //load value from other cached resources with same neutral value whose same lang translation is not null
                        var value = values[res.Index];

                        //if we dont have such translation then continue
                        if (value == null)
                            continue;

                        //update the resource file
                        resourceEntry.Values.SetValue(lang.Culture, value[lang.Culture.Name]);
                        resourceEntry.Comments.SetValue(lang.Culture, "@autotranslated");

                        count++;
                    }
                }
            }

            //save changes in resource
            manager.Save();

            OnFinished?.Invoke();
        }
        public delegate void OnProgressHandler(ProgressEventArg e);
        public static event OnProgressHandler? OnProgress;

        public delegate void OnFinishedHandler();
        public static event OnFinishedHandler? OnFinished;

        public delegate Task<RequireActionResult> OnRequireActionHandler(RequireActionEventArg e);
        public static event OnRequireActionHandler? OnTranslationAction;
    }

    public class ProgressEventArg
    {
        public EventType EventType { get; set; }
        public int CachedItemCount { get; set; }
    }
    public class RequireActionEventArg
    {
        public string Key { get; set; }
        public string UniqueName { get; set; }
        public string ProjectName { get; set; }
        public IReadOnlyList<TranslateContainerModel> Values { get; set; }
        public string Culture { get; internal set; }
        public string NeutralText { get; set; }
    }
    public struct RequireActionResult
    {
        public bool Merge { get; set; }
        public int Index { get; set; }
    }
    public enum EventType
    {
        CacheBuildUp
    }
}
