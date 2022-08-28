using System.Diagnostics;
using System.Net;
using HtmlAgilityPack;

// Key is ItemID
Dictionary<string, List<SkinEntry>> itemSkins = new();

HttpClient client = new HttpClient();

// Get html page of rustlab's skins site
string skins = await client.GetStringAsync("https://rustlabs.com/skins");


// Load the html page into a doc
HtmlDocument skinsDOM = new HtmlDocument();
skinsDOM.LoadHtml(skins);
// Select each item skin card
List<HtmlNode> nodes = skinsDOM.DocumentNode.SelectNodes("//*[@id=\"wrappah\"]/a").ToList();

// Store total skins for progress report
int totalSkins = nodes.Count;

// For each card
foreach (var node in nodes) {
    // Extra attributes and store them in SkinEntry
    var attribs = node.Attributes;
    SkinEntry entry = new SkinEntry();
    entry.SkinName = attribs["data-name"].Value;
    entry.ItemName = attribs["data-item"].Value;
    entry.ItemId = attribs["data-group"].Value;
    entry.EntryLink = "https:" + attribs["href"].Value;
    // If it's a new item id, initialize it's SkinEntry list
    if (!itemSkins.ContainsKey(entry.ItemId)) {
        itemSkins[entry.ItemId] = new List<SkinEntry>();
    }
    // Add the skin to our dict
    itemSkins[entry.ItemId].Add(entry);
}

// We need to convert from item ID to the item's shortnames
Dictionary<string, string> idToShortnames = new();
// Using corrosionhour's item table as the shortnames aren't provided to us on rustlabs
string shortnames = await client.GetStringAsync("https://www.corrosionhour.com/rust-item-list/");

// Load the html page into a doc
HtmlDocument shortnamesDOM = new HtmlDocument();
shortnamesDOM.LoadHtml(shortnames);

// Select the table html element
HtmlNode itemTable = shortnamesDOM.DocumentNode.SelectSingleNode("/html/body/div/div/div/div/main/article/div/div/table/tbody");

foreach (var item in itemTable.ChildNodes) {
    // Each item holds both item ID and the item's shortname, allowing us to add entries easily
    idToShortnames[item.ChildNodes[2].InnerHtml] = item.ChildNodes[1].InnerHtml;
}

// The starting boilerplate of Skins.json
string startConfig = @"{
  ""Commands"": [
    ""skin"",
    ""skins""
  ],
  ""Skins"": [
";
// Ending boilerplate of Skins.json
string endConfig = @"  ],
  ""Container Panel Name"": ""generic"",
  ""Container Capacity"": 36,
  ""UI"": {
    ""Background Color"": ""0.18 0.28 0.36"",
    ""Background Anchors"": {
        ""Anchor Min X"": ""1.0"",
      ""Anchor Min Y"": ""1.0"",
      ""Anchor Max X"": ""1.0"",
      ""Anchor Max Y"": ""1.0""
    },
    ""Background Offsets"": {
    ""Offset Min X"": ""-300"",
      ""Offset Min Y"": ""-100"",
      ""Offset Max X"": ""0"",
      ""Offset Max Y"": ""0""
    },
    ""Left Button Text"": ""<size=36><</size>"",
    ""Left Button Color"": ""0.11 0.51 0.83"",
    ""Left Button Anchors"": {
    ""Anchor Min X"": ""0.025"",
      ""Anchor Min Y"": ""0.05"",
      ""Anchor Max X"": ""0.325"",
      ""Anchor Max Y"": ""0.95""
    },
    ""Center Button Text"": ""<size=36>Page: {page}</size>"",
    ""Center Button Color"": ""0.11 0.51 0.83"",
    ""Center Button Anchors"": {
    ""Anchor Min X"": ""0.350"",
      ""Anchor Min Y"": ""0.05"",
      ""Anchor Max X"": ""0.650"",
      ""Anchor Max Y"": ""0.95""
    },
    ""Right Button Text"": ""<size=36>></size>"",
    ""Right Button Color"": ""0.11 0.51 0.83"",
    ""Right Button Anchors"": {
    ""Anchor Min X"": ""0.675"",
      ""Anchor Min Y"": ""0.05"",
      ""Anchor Max X"": ""0.975"",
      ""Anchor Max Y"": ""0.95""
    }
}
}";

// String to store the formatted data we need
string skinEntries = "";
// Current progress counter
int current = 0;

Stopwatch stopwatch = Stopwatch.StartNew();

// Iterates over each item (not skin)
for (int i = 0; i < itemSkins.Count; i++) {
    // If the item doesn't exist in our idToShortnames dict, skip it
    // This can be true for things such as the twitch rival trophy and other brand new items that don't have a default base variant
    if (!idToShortnames.ContainsKey(itemSkins.Keys.ToList()[i])) continue;
    // If the item doesn't have any skins, skip it
    if (itemSkins[itemSkins.Keys.ToList()[i]].Count == 0) continue;

    // Boilerplate for new item entry in Skins.json
    skinEntries += $"    {{\n      \"Item Shortname\": \"{idToShortnames[itemSkins.Keys.ToList()[i]]}\",\n      \"Permission\": \"\",\n      \"Skins\": [\n        0,\n";
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
            skinEntries += $"        {workshopNode.InnerHtml}{(j == itemSkins[itemSkins.Keys.ToList()[i]].Count - 1 ? "" : ",")}\n";
        } else {
            // Otherwise just print out which item doesn't have a workshop id
            Console.WriteLine($"Failed to get workshop ID of {entry}");
        }
        // Increment our current progress counter
        current++;
    }
    // End this item's skin entries
    skinEntries += $"      ]\n    }}{(i == itemSkins.Keys.Count - 1 ? "" : ",")}\n";
    Console.WriteLine(((float)current / totalSkins).ToString("P"));
}

// Write our data to a file
File.WriteAllText("Skins.json", startConfig + skinEntries + endConfig);

Console.WriteLine($"Done! Took {stopwatch.Elapsed.ToString()} to complete.");
Console.Read();


struct SkinEntry {
    public string SkinName;
    public string ItemName;
    public string ItemId;
    public string EntryLink;
    public override string ToString() {
        return $"Skin: {SkinName} Item: {ItemName} ItemId: {ItemId} EntryLink: {EntryLink}";
    }
}