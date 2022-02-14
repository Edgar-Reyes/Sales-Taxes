using Microsoft.AspNetCore.Mvc;
using Sales.Models;
using System.Diagnostics;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;
using System.Text.RegularExpressions;

namespace Sales.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BadInputFormat"></param>
        /// <param name="Receipt"></param>
        /// <returns></returns>
        public IActionResult Index(bool BadInputFormat = false, string? Receipt = null)
        {
            var itemsList = GetItemList();
            ViewBag.BadInputFormat = (BadInputFormat) ? "true" : null;
            ViewBag.Receipt = Receipt;
            return View(itemsList);
        }

        #region MethodByForm

        /// <summary>
        /// Saves the new item if it's valid
        /// </summary>
        /// <param name="newItem"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SaveItemForm(ItemModel newItem)
        {
            if (ModelState.IsValid)
            {
                SaveNewItem(newItem);
            }
            return RedirectToAction("Index");
        }

        #endregion

        #region Receipt

        /// <summary>
        /// Generates the receipt and callback the result
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult PrintReceiptForm()
        {
            return RedirectToAction("Index", new { Receipt = GenerateReceipt() });
        }

        /// <summary>
        /// Generates the receipt with the required format
        /// </summary>
        /// <returns></returns>
        private string GenerateReceipt()
        {
            var receipt = string.Empty;
            var itemsList = GetItemList();
            var itemGroups = itemsList?.GroupBy(x => new { x.Imported, x.Name, x.FinalPrice }).Select(y => y.First()).ToList();
            var itemCount = 0;
            var groupTotal = 0.00M;
            var receiptLine = string.Empty;
            var importedLegend = string.Empty;
            var countLegend = string.Empty;

            foreach (var group in itemGroups)
            {
                itemCount = itemsList.Where(x => x.Name == group.Name && x.Imported == group.Imported && x.FinalPrice == group.FinalPrice).Count();
                groupTotal = itemsList.Sum(x => x.FinalPrice);
                importedLegend = (group.Imported) ? "Imported " : string.Empty;
                countLegend = (itemCount > 1) ? $" ({itemCount} @ {group.FinalPrice})" : string.Empty;
                receiptLine = $"{importedLegend}{group.Name}: {itemCount * group.FinalPrice}{countLegend}\n";
                receipt += receiptLine;
            }

            receipt += $"Sales Taxes: {itemsList.Sum(x => x.RoundedSalesTax + x.RoundedImportedTax)}\n";
            receipt += $"Total: {itemsList.Sum(x => x.Total)}\n";

            return receipt;
        }

        #endregion


        /// <summary>
        /// Retrieves items list add the new one and save changes in items.json
        /// </summary>
        /// <param name="newItem"></param>
        private void SaveNewItem(ItemModel newItem)
        {
            var itemList = GetItemList();
            itemList?.Add(newItem);
            SaveItems(itemList);
        }

        #region MethodByInputLine

        /// <summary>
        /// Determines if an item is Imported or not
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsImported(string name)
        {
            return name.Contains("Imported");
        }

        /// <summary>
        /// Determines if an item is exempted of Sales Tax
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsExempted(string name)
        {
            string pattern = @"(book|chocolate|pill)";
            Regex rgx = new Regex(pattern);
            Match match = rgx.Match(name.ToLower());

            return match.Success;
        }

        private string GetItemName(string name)
        {
            return name.Replace("Imported", string.Empty).Trim();
        }

        /// <summary>
        /// Uses regex to valid and convert input line to a ItemModel object. Calls SaveNewItem method to add the new Item and save changes in item.json file
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SaveInputForm(string Input)
        {
            if (Input == null) return RedirectToAction("Index", new { BadInputFormat = true });
            string pattern = @"(?<quantity>[0-9]+) (?<name>.*) (at) (?<price>[0-9]+.[0-9]+)";
            bool BadInputFormat = false;
            Regex rgx = new Regex(pattern);
            Match match = rgx.Match(Input);
            if (match.Success)
            {
                Group quantity = match.Groups["quantity"];
                Group name = match.Groups["name"];
                Group price = match.Groups["price"];

                var newItem = new ItemModel
                {
                    Imported = IsImported(name.Value),
                    Exempted = IsExempted(name.Value),
                    Name = GetItemName(name.Value),
                    Quantity = int.Parse(quantity.Value),
                    Price = decimal.Parse(price.Value),
                };

                SaveNewItem(newItem);
            }
            else
            {
                BadInputFormat = true;
            }

            return RedirectToAction("Index", new { BadInputFormat });
        }

        #endregion

        #region JsonFileHandlers

        /// <summary>
        /// Removes all lines from items.json file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult RemoveItemsForm()
        {
            System.IO.File.WriteAllText(@"Database\items.json", string.Empty);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Reads item.json file and deserialize to an ItemModel list to handle it
        /// </summary>
        /// <returns>List of ItemModel</returns>
        private List<ItemModel>? GetItemList()
        {
            var jsonFile = System.IO.File.ReadAllText(@"Database\items.json");
            return JsonConvert.DeserializeObject<List<ItemModel>>(jsonFile) ?? new List<ItemModel>();
        }

        /// <summary>
        /// Save a item ist into items.json file
        /// </summary>
        /// <param name="itemList"></param>
        private void SaveItems(List<ItemModel>? itemList)
        {
            var convertedJson = JsonConvert.SerializeObject(itemList);
            System.IO.File.WriteAllText(@"Database\items.json", convertedJson);
        }

        #endregion
    }
}