using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;

class Program
{
	static async Task Main(string[] args)
	{
		var baseUrl = "https://izoh.uz/letter/A?size=500&page=";
		var filePath = "D:\\Mobile\\New folder\\attackizohuz\\attackizohuz\\filewords.json";

		// 4 marta siklni ishlatish
		for (int i = 0; i < 4; i++)
		{
			var url = $"{baseUrl}{i + 1}";

			using (var client = new HttpClient()) // HttpClient 'using' operatori bilan ishlatiladi
			{
				try
				{
					// HTTP so'rov yuborish
					var response = await client.GetStringAsync(url);
					var htmlDoc = new HtmlDocument();
					htmlDoc.LoadHtml(response);

					var links = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class,'py-2 pr-4')]");

					var allLinksData = new List<Dictionary<string, string>>();

					// Link ma'lumotlarini yig'ish
					foreach (var link in links)
					{
						var linkData = new Dictionary<string, string>
						{
							{ "text", link.InnerText.Trim() },
							{ "href", link.GetAttributeValue("href", "") }
						};
						allLinksData.Add(linkData);
					}

					var allWordsData = new List<string>();
					foreach (var linkData in allLinksData)
					{
						var linkUrl = linkData["href"];
						var linkResponse = await client.GetStringAsync(linkUrl);
						var linkDoc = new HtmlDocument();
						linkDoc.LoadHtml(linkResponse);

						// Kerakli ma'lumotlarni olish
						var h1Text = linkDoc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "";
						var italicText = linkDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'italic')]")?.InnerText ?? "";
						var boldText = linkDoc.DocumentNode.SelectSingleNode("//p[contains(@class,'font-bold')]")?.InnerText ?? "";

						var data = $"{h1Text}||{italicText}||{boldText}";
						allWordsData.Add(data);
					}

					// JSON fayliga saqlash
					var dataToSave = new List<string>();
					if (File.Exists(filePath))
					{
						var existingData = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath)) ?? [];
						existingData.AddRange(allWordsData);
						dataToSave = existingData;
					}
					else
					{
						dataToSave = allWordsData;
					}

					// Faylga saqlash
					File.WriteAllText(filePath, JsonConvert.SerializeObject(dataToSave, Formatting.Indented));

					Console.WriteLine($"Batch {i + 1}: Barcha <a> teglar muvaffaqiyatli saqlandi.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Batch {i + 1}: Xatolik yuz berdi - {ex.Message}");
				}
			}
		}
	}
}
