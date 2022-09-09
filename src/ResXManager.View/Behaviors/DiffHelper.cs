using ResXManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResXManager.View.Behaviors
{
    internal class DiffHelper
    {
        public static HashSet<ResourceTableEntry> CalcDiff(ResourceManager _resourceManager)
        {
            var allChanges = new HashSet<ResourceTableEntry>();
            var entries = _resourceManager.TableEntries.GroupBy(x => x.Container);
            foreach (var gp in entries)
            {
                foreach (var resource in gp)
                {
                    foreach (var lang in resource.Languages)
                    {
                        var snap = resource.SnapshotValues.GetValue(lang.Culture);
                        var value = resource.Values.GetValue(lang.Culture);

                        if (string.IsNullOrWhiteSpace(snap) && string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        if ((string.IsNullOrWhiteSpace(snap) && !string.IsNullOrWhiteSpace(value)) ||
                            (!string.IsNullOrWhiteSpace(snap) && string.IsNullOrWhiteSpace(value)) ||
                            !snap.Equals(value, StringComparison.Ordinal))
                        {
                            allChanges.Add(resource);
                            break;
                        }
                    }
                }
            }
            return allChanges;
        }

    }
}
