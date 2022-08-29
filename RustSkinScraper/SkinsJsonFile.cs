using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustSkinScraper {
    internal class SkinsJsonFile {
        // https://umod.org/plugins/skins#configuration
        private class Configuration {
            [JsonProperty(PropertyName = "Commands")]
            public string[] Commands = { "skin", "skins" };

            [JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<SkinItem> Skins = new List<SkinItem> { new SkinItem() };

            [JsonIgnore]
            public Dictionary<string, List<SkinItem>> IndexedSkins = new Dictionary<string, List<SkinItem>>();

            [JsonProperty(PropertyName = "Container Panel Name")]
            public string Panel = "generic";

            [JsonProperty(PropertyName = "Container Capacity")]
            public int Capacity = 36;

            [JsonProperty(PropertyName = "UI")]
            public UIConfiguration UI = new UIConfiguration();

            public class SkinItem {
                [JsonProperty(PropertyName = "Item Shortname")]
                // ReSharper disable once MemberCanBePrivate.Local
                public string Shortname = "shortname";

                [JsonProperty(PropertyName = "Permission")]
                public string Permission = "";

                [JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<ulong> Skins = new List<ulong> { 0 };
            }

            public class UIConfiguration {
                [JsonProperty(PropertyName = "Background Color")]
                public string BackgroundColor = "0.18 0.28 0.36";

                [JsonProperty(PropertyName = "Background Anchors")]
                public Anchors BackgroundAnchors = new Anchors { AnchorMinX = "1.0", AnchorMinY = "1.0", AnchorMaxX = "1.0", AnchorMaxY = "1.0" };

                [JsonProperty(PropertyName = "Background Offsets")]
                public Offsets BackgroundOffsets = new Offsets { OffsetMinX = "-300", OffsetMinY = "-100", OffsetMaxX = "0", OffsetMaxY = "0" };

                [JsonProperty(PropertyName = "Left Button Text")]
                public string LeftText = "<size=36><</size>";

                [JsonProperty(PropertyName = "Left Button Color")]
                public string LeftColor = "0.11 0.51 0.83";

                [JsonProperty(PropertyName = "Left Button Anchors")]
                public Anchors LeftAnchors = new Anchors { AnchorMinX = "0.025", AnchorMinY = "0.05", AnchorMaxX = "0.325", AnchorMaxY = "0.95" };

                [JsonProperty(PropertyName = "Center Button Text")]
                public string CenterText = "<size=36>Page: {page}</size>";

                [JsonProperty(PropertyName = "Center Button Color")]
                public string CenterColor = "0.11 0.51 0.83";

                [JsonProperty(PropertyName = "Center Button Anchors")]
                public Anchors CenterAnchors = new Anchors { AnchorMinX = "0.350", AnchorMinY = "0.05", AnchorMaxX = "0.650", AnchorMaxY = "0.95" };

                [JsonProperty(PropertyName = "Right Button Text")]
                public string RightText = "<size=36>></size>";

                [JsonProperty(PropertyName = "Right Button Color")]
                public string RightColor = "0.11 0.51 0.83";

                [JsonProperty(PropertyName = "Right Button Anchors")]
                public Anchors RightAnchors = new Anchors { AnchorMinX = "0.675", AnchorMinY = "0.05", AnchorMaxX = "0.975", AnchorMaxY = "0.95" };

                [JsonIgnore]
                public string ParsedUI;

                [JsonIgnore]
                public int IndexPagePrevious, IndexPageCurrent, IndexPageNext;

                public class Anchors {
                    [JsonProperty(PropertyName = "Anchor Min X")]
                    public string AnchorMinX = "0.0";

                    [JsonProperty(PropertyName = "Anchor Min Y")]
                    public string AnchorMinY = "0.0";

                    [JsonProperty(PropertyName = "Anchor Max X")]
                    public string AnchorMaxX = "1.0";

                    [JsonProperty(PropertyName = "Anchor Max Y")]
                    public string AnchorMaxY = "1.0";

                    [JsonIgnore]
                    public string AnchorMin => $"{AnchorMinX} {AnchorMinY}";

                    [JsonIgnore]
                    public string AnchorMax => $"{AnchorMaxX} {AnchorMaxY}";
                }

                public class Offsets {
                    [JsonProperty(PropertyName = "Offset Min X")]
                    public string OffsetMinX = "0";

                    [JsonProperty(PropertyName = "Offset Min Y")]
                    public string OffsetMinY = "0";

                    [JsonProperty(PropertyName = "Offset Max X")]
                    public string OffsetMaxX = "100";

                    [JsonProperty(PropertyName = "Offset Max Y")]
                    public string OffsetMaxY = "100";

                    [JsonIgnore]
                    public string OffsetMin => $"{OffsetMinX} {OffsetMinY}";

                    [JsonIgnore]
                    public string OffsetMax => $"{OffsetMaxX} {OffsetMaxY}";
                }
            }
        }

        public static async Task<string> WriteConfigAsync(Dictionary<string, string> idToShortnames, Dictionary<string, List<SkinEntry>> itemSkins) {
            Configuration cfg = new Configuration();
            HttpClient client = new HttpClient();

            // Iterates over each item (not skin)
            for (int i = 0; i < itemSkins.Count; i++) {
                // If the item doesn't exist in our idToShortnames dict, skip it
                // This can be true for things such as the twitch rival trophy and other brand new items that don't have a default base variant
                if (!idToShortnames.ContainsKey(itemSkins.Keys.ToList()[i])) continue;
                // If the item doesn't have any skins, skip it
                if (itemSkins[itemSkins.Keys.ToList()[i]].Count == 0) continue;

                // Create a SkinItem for the item
                Configuration.SkinItem skinItem = new Configuration.SkinItem();
                skinItem.Shortname = idToShortnames[itemSkins.Keys.ToList()[i]];

                // Iterates over each skin entry
                for (int j = 0; j < itemSkins[itemSkins.Keys.ToList()[i]].Count; j++) {
                    SkinEntry entry = itemSkins[itemSkins.Keys.ToList()[i]][j];

                    // Get the skin ID from the entry's link
                    string skinDetails = await client.GetStringAsync(entry.EntryLink);
                    HtmlDocument detailsDOM = new HtmlDocument();
                    detailsDOM.LoadHtml(skinDetails);
                    // Select the ID html element
                    HtmlNode workshopNode = detailsDOM.DocumentNode.SelectSingleNode("/html/body/div[1]/div[2]/div/table/tbody/tr[4]/td[2]/a");

                    // If the skin has an ID (only true if it was on the workshop before being added to the game)
                    if (workshopNode != null) {
                        // Add it to our formatted data string
                        skinItem.Skins.Add(ulong.Parse(workshopNode.InnerHtml));
                    } else {
                        // Otherwise just print out which item doesn't have a workshop id
                        Console.WriteLine($"Failed to get workshop ID of {entry}");
                    }
                }
            }

            return JsonConvert.SerializeObject(cfg, Formatting.Indented);
        }
    }
}
