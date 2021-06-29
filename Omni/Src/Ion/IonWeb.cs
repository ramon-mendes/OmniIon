#if APP_MVC || MVC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Ion
{
	public static class IonWeb
	{
		private static Dictionary<QueryedUser, DateTime> _cache;

		private struct QueryedUser
		{
			public EProduct ep;
			public string hh64;
			public string @as;
		}

		public static bool CheckQueryingUser(EProduct ep, string hh64, string @as)
		{
			var query = new QueryedUser()
			{
				ep = ep,
				hh64 = hh64,
				@as = @as
			};

			if(_cache.ContainsKey(query))
			{
				var expires = _cache[query];
				if(DateTime.Now <= expires)
					return true;

				// expired
				_cache.Remove(query);
			}

			using(HttpClient hc = new HttpClient())
			{
				try
				{
					var response = hc.GetAsync(IonConsts.SERVER + $"Ion/IsAuthorized?ep={query.ep}&hh64={query.hh64}&as={query.@as}").Result;
					response.EnsureSuccessStatusCode();
					string res = response.Content.ReadAsStringAsync().Result;
					bool ok = res == "True";
					if(ok)
					{
						_cache.Add(query, DateTime.Now.AddHours(5));
					}
					return ok;
				}
				catch(Exception)
				{
					return false;
				}
			}
		}

		public static void SaveDownloadingUser(EProduct ep, string ip, string name, string email)
		{
			Task.Run(() =>
			{
				using(HttpClient hc = new HttpClient())
				{
					var response = hc.GetAsync(IonConsts.SERVER + $"Ion/SaveDownloadingUser?ep={EProduct.DESIGNARSENAL}&ip={Uri.EscapeDataString(ip)}&name={Uri.EscapeDataString(name)}&email={Uri.EscapeDataString(email)}").Result;
					response.EnsureSuccessStatusCode();
				}
			});
		}
	}

	public class DownloadingUser
	{
		public EProduct ep { get; set; }
		public string ip { get; set; }
		public string name { get; set; }
		public string email { get; set; }
	}
}
#endif