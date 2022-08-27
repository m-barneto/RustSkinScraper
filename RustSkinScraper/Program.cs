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
foreach (var item in itemSkins.Keys) {
    /*
    {
      "Item Shortname": "shortname",
      "Permission": "",
      "Skins": [
        0
      ]
    }
     */
    skinEntries += $"    {{\n      \"Item Shortname\": \"{idToShortnames[item]}\",\n      \"Permission\": \"\",\n      \"Skins\": [";
    for (int i = 0; i < itemSkins[item].Count; i++) {
        SkinEntry entry = itemSkins[item][i];
        // get workshop id

        skinEntries += $"        {"workshop id here"},\n      ]\n    }}{(i == itemSkins[item].Count - 1 ? "," : "")}";
    }
}

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