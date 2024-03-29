﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Type;

public class QuoteService
{
	private static HttpClient _httpClient = new();
	private static int _rng = -1;
	//private static List<Quote> _quotes ;
	private static Stack<Quote> _stackQuotes;
	public static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
	{
		try
		{
			url ??= CultureInfo.InstalledUICulture switch
			{
				{ Name: var n } when n.StartsWith("fa") => // Iran
					"http://www.aparat.com",
				{ Name: var n } when n.StartsWith("zh") => // China
					"http://www.baidu.com",
				_ =>
					"http://www.gstatic.com/generate_204",
			};

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.KeepAlive = false;
			request.Timeout = timeoutMs;
			using (var response = (HttpWebResponse)request.GetResponse())
				return true;
		}
		catch
		{
			return false;
		}
	}
	public static async Task GetQuotes()
	{
		if (_stackQuotes == null)
		{
			if (CheckForInternetConnection())
			{
				var _quotes = new List<Quote>();
				_stackQuotes = new Stack<Quote>();
				for (var i = 0; i < 3; i++)
				{
					var res = await _httpClient.GetAsync("https://zenquotes.io/api/quotes");
					var quotes = await res.Content.ReadFromJsonAsync<List<Quote>>();
					_quotes!.AddRange(quotes!);
					
				}
				_stackQuotes = new Stack<Quote>(_quotes);
			}
			else
			{
				var path = Windows.ApplicationModel.Package.Current.InstalledPath + "\\quotes.json";
				var json = File.ReadAllText(path);
				//_quotes = JsonSerializer.Deserialize<List<Quote>>(json);
				_stackQuotes = JsonSerializer.Deserialize<Stack<Quote>>(json);
			}
		}
	}
	public static async Task<Quote> GetQuote()
	{
		
		// use stack
		var qu = new Stack<Quote>();
		
		//var path = Windows.ApplicationModel.Package.Current.InstalledPath + "\\quotes.json";
		//var json = File.ReadAllText(path);
		//var res = JsonSerializer.Deserialize<Quotes>(json);
		/*
		var rnd = new Random();
		var num = rnd.Next(0, _quotes.Count);
		while (_rng == num)
		{
			num = rnd.Next(0, _quotes.Count);
		}
		_rng = num;
		return _quotes[num];*/
		if (_stackQuotes.Count == 0)
			await GetQuotes();
		return _stackQuotes.Pop();
	}
}

public class Quote
{
	[JsonPropertyName("q")]
	public string quote { get; set; }
	[JsonPropertyName("a")]
	public string author { get; set; }
}