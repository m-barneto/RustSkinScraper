using System.Diagnostics;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RustSkinScraper;


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

Stopwatch stopwatch = Stopwatch.StartNew();

string skinsConfig = await SkinsJsonFile.WriteConfigAsync(idToShortnames, itemSkins);

File.WriteAllText("Skins.json", skinsConfig);

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