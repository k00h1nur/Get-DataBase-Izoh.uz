using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Collections.Generic;

class Program
{
	private static readonly HttpClient client = new HttpClient();

	static async Task<int> Main(string[] args)
	{
		List<string> args1 = new List<string>() { "X","Y","Z", "O‘", "G‘","Sh","Ch"};
		foreach (var arg in args1)
		{

			var baseUrl = "https://izoh.uz/letter/"+$"{arg}"+"?size=500&page=";
			var filePath = "C:\\Users\\user\\Documents\\Dialectics\\Dictionary-of-Uzbek-dialects\\DataBase\\AddWordsLatters.sql";

			int i = 0;
			while(true)
			{
				var url = $"{baseUrl}{i + 1}";

				try
				{
					// HTTP so'rov yuborish va javobni tekshirish
					using (var response = await client.GetAsync(url))
					{
						if (!response.IsSuccessStatusCode)
						{
							Console.WriteLine($"Xatolik: {url} - Status code: {response.StatusCode}");
							break;
						}

						var responseContent = await response.Content.ReadAsStringAsync();
						var htmlDoc = new HtmlDocument();
						htmlDoc.LoadHtml(responseContent);

						var links = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class,'py-2 pr-4')]");
						if (links is null)
						{
							break;
						}
						var sqlInserts = new List<string>();
						sqlInserts.Add("INSERT INTO LiteraryWords (Title,[Description],PartOfSpeechId) VALUES");

						foreach (var link in links)
						{
							var linkUrl = link.GetAttributeValue("href", "");

							// Har bir havola uchun HTTP so'rov yuborish
							using (var linkResponse = await client.GetAsync(linkUrl))
							{
								if (!linkResponse.IsSuccessStatusCode)
								{
									Console.WriteLine($"Xatolik: {linkUrl} - Status code: {linkResponse.StatusCode}");
									continue;
								}

								var linkContent = await linkResponse.Content.ReadAsStringAsync();
								var linkDoc = new HtmlDocument();
								linkDoc.LoadHtml(linkContent);

								// Kerakli ma'lumotlarni olish
								var h1Text = linkDoc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim() ?? "";
								var italicText = linkDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'italic')]")?.InnerText?.Trim() ?? "";
								var italicnum = italicText switch
								{
									"ot" => 1,
									"sf" => 2,
									"sn" => 3,
									"fl" => 4,
									"ys" => 2,
									"rv" => 6,
									"ol" => 5,
									_ => 1,
								};
								var boldText = linkDoc.DocumentNode.SelectSingleNode("//p[contains(@class,'font-bold')]")?.InnerText?.Trim() ?? "";
								boldText = boldText.StartsWith('1') ? boldText.Replace("1. ", "") : boldText;

								// SQL buyruqni yaratish
								var sql = $"('{h1Text.Replace("'", "''")}', '{boldText.Replace("'", "''")}', {italicnum}),";
								sqlInserts.Add(sql);
							}
						}

						// Faylga yozish
						await File.AppendAllLinesAsync(filePath, sqlInserts);
						Console.WriteLine($"Batch {i + 1}: Ma'lumotlar muvaffaqiyatli faylga yozildi.");
					}
				}
				catch (Exception ex)
				{
					// Xatolikni logga yozish
					await File.AppendAllTextAsync(filePath, ex.Message.ToString());
					Console.WriteLine($"Batch {i + 1}: Xatolik yuz berdi - {ex.Message}");
				}
				i++;
			}

			// Konsol oynasini ushlab turish
			Console.WriteLine("Ish yakunlandi. Davom etish uchun istalgan tugmani bosing...");
			Console.ReadKey();
		}
		return 0;
	}
}
