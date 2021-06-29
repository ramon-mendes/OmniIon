using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Kernys.Bson;
#if APP_AC
using ATCommander;
#elif APP_ID
using IconDrop;
#elif APP_PD
using PatternDrop;
#elif APP_FD
using FontDrop;
#elif APP_DA
using DesignArsenal;
#elif APP_OMNI
using Omni;
#elif APP_OMNICODE
using OmniCode;
#elif APP_DSA
using DeveloperArsenal;
#elif APP_STICKY_NOTES
using StickyNotes;
#endif

namespace Ion
{
	[Obfuscation(Exclude = true)]
	class UpdateInfo
	{
		public int v = 0;
		public int n = 0;// NTP
		public int r = 0;// last update date
	}

	static class UpdateControl
	{
		private static string PathUpdateInfo = Consts.DirUserData + "update.json";

		public static void Setup()
		{
#if DEBUG
			//File.Delete(PathUpdateInfo);
#endif

			var t = new Thread(() =>
			{
#if !DEBUG
				if(!Debugger.IsAttached)
					Thread.Sleep(TimeSpan.FromMinutes(2));
#endif

				while(true)
				{
					try
					{
						ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
						byte[] bson = new WebClient().DownloadData(IonConsts.URL_APPINFO + "?ep=" + (int)Consts.ProductID);

						if(Encoding.UTF8.GetString(bson.Take(6).ToArray()) != "<html>")
						{
							File.WriteAllBytes(PathUpdateInfo, bson);
						}
					}
					catch(Exception ex)
					{
						if(Debugger.IsAttached)
						{
							//Debug.Assert(false);
						}
					}
					Thread.Sleep(TimeSpan.FromMinutes(9));
				}
			});
            t.IsBackground = true;
            t.Start();
		}

		private static UpdateInfo ReadInfo()
		{
			int XOR_CYPHER = IonConsts.XOR[(int) Consts.ProductID];
			var bson = SimpleBSON.Load(File.ReadAllBytes(PathUpdateInfo));
			UpdateInfo upinfo = new UpdateInfo()
			{
				v = bson["v"].int32Value ^ XOR_CYPHER,// last version
				n = bson["n"].int32Value ^ XOR_CYPHER,// NTP
				//r = bson["r"].int32Value^ XOR_CYPHER,// last update date
			};
			return upinfo;
		}

		public static DateTime GetDateTime()
		{
			if(!File.Exists(PathUpdateInfo))
				return DateTime.Now;

			int epoch = ReadInfo().n;
			DateTime dt_ntp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
			if(DateTime.UtcNow > dt_ntp)
				dt_ntp = DateTime.UtcNow;
			return dt_ntp;
		}

		public static string IsUpdateAvailable()
		{
			if(!File.Exists(PathUpdateInfo))
				return null;

			int v = ReadInfo().v;
			if(Consts.VersionInt < v)
			{
				int major = v >> 16;
				int minor = v & 0xFFFF;
				string lastVersion = major + "." + minor;
				return lastVersion;
			}
			return null;
		}
	}
}