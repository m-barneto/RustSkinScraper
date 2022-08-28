using System.Net;
using HtmlAgilityPack;


Dictionary<string, List<SkinEntry>> itemSkins = new();

HttpClient client = new HttpClient();
string skins = await client.GetStringAsync("https://rustlabs.com/skins");


HtmlDocument skinsDOM = new HtmlDocument();
skinsDOM.LoadHtml(skins);
List<HtmlNode> nodes = skinsDOM.DocumentNode.SelectNodes("//*[@id=\"wrappah\"]/a").ToList();

foreach (var node in nodes) {
    var attribs = node.Attributes;
    SkinEntry entry = new SkinEntry();
    entry.SkinName = attribs["data-name"].Value;
    entry.ItemName = attribs["data-item"].Value;
    entry.ItemId = attribs["data-group"].Value;
    entry.EntryLink = attribs["href"].Value;
    if (!itemSkins.ContainsKey(entry.ItemId)) {
        itemSkins[entry.ItemId] = new List<SkinEntry>();
    }
    itemSkins[entry.ItemId].Add(entry);
}


Dictionary<string, string> idToShortnames = new();

string shortnames = await client.GetStringAsync("https://www.corrosionhour.com/rust-item-list/");

HtmlDocument shortnamesDOM = new HtmlDocument();
shortnamesDOM.LoadHtml(shortnames);

HtmlNode itemTable = shortnamesDOM.DocumentNode.SelectSingleNode("/html/body/div/div/div/div/main/article/div/div/table/tbody");

foreach (var item in itemTable.ChildNodes) {
    idToShortnames[item.ChildNodes[2].InnerHtml] = item.ChildNodes[1].InnerHtml;
}


string startConfig = @"{
  ""Commands"": [
    ""skin"",
    ""skins""
  ],
  ""Skins"": [
";
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

string skinEntries = "";
int current = 0;

for (int i = 0; i < itemSkins.Count; i++) {
    /*
    {
      "Item Shortname": "shortname",
      "Permission": "",
      "Skins": [
        0
      ]
    }
     */
    if (!idToShortnames.ContainsKey(itemSkins.Keys.ToList()[i])) continue;
    if (itemSkins[itemSkins.Keys.ToList()[i]].Count == 0) continue;
    skinEntries += $"    {{\n      \"Item Shortname\": \"{idToShortnames[itemSkins.Keys.ToList()[i]]}\",\n      \"Permission\": \"\",\n      \"Skins\": [\n";
    for (int j = 0; j < itemSkins[itemSkins.Keys.ToList()[i]].Count; j++) {
        SkinEntry entry = itemSkins[itemSkins.Keys.ToList()[i]][j];
        // get workshop id
        string skinDetails = await client.GetStringAsync("https:" + entry.EntryLink);
        HtmlDocument detailsDOM = new HtmlDocument();
        detailsDOM.LoadHtml(skinDetails);

        HtmlNode workshopNode = detailsDOM.DocumentNode.SelectSingleNode("/html/body/div[1]/div[2]/div/table/tbody/tr[4]/td[2]/a");
        if (workshopNode != null) {
            skinEntries += $"        {workshopNode.InnerHtml}{(j == itemSkins[itemSkins.Keys.ToList()[i]].Count - 1 ? "" : ",")}\n";
        } else {
            Console.WriteLine($"Failed to get workshop ID of {entry.SkinName} : {"https:" + entry.EntryLink}");
        }
        current++;
        Console.WriteLine(current + " / " + 4300 + " something");
    }
    skinEntries += $"      ]\n    }}{(i == itemSkins.Keys.Count - 1 ? "" : ",")}\n";
}

File.WriteAllText("Skins.json", startConfig + skinEntries + endConfig);

Console.ReadLine();


struct SkinEntry {
    public string SkinName;
    public string ItemName;
    public string ItemId;
    public string EntryLink;
    public string ToString() {
        return $"Skin: {SkinName} Item: {ItemName} ItemId: {ItemId} EntryLink: {EntryLink}";
    }
}