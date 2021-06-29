using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kernys.Bson;
using SciterSharp;

#if APP_MVC
using IonMVC.Classes;
#elif APP_AC
using ATCommander;
using ATCommander.Hosting;
#elif APP_ID
using IconDrop;
using IconDrop.Hosting;
#elif APP_PD
using PatternDrop;
using PatternDrop.Hosting;
#elif APP_FD
using FontDrop;
using FontDrop.Hosting;
#elif APP_DA
using DesignArsenal;
using DesignArsenal.Hosting;
#elif APP_OMNI
using Omni;
using Omni.Hosting;
#elif APP_OMNICODE
using OmniCode;
using OmniCode.ToolWnd;
#elif APP_DSA
using DeveloperArsenal;
using DeveloperArsenal.Hosting;
#endif

namespace Ion
{
	static class IonApp
	{
		public static readonly string HID;
		public static string URIAuthData => $"ep={(int)Consts.ProductID}&hh64={Uri.EscapeDataString(HIDHash64)}&as={Uri.EscapeDataString(ActivationSignature)}";

		public static readonly string PathActivationInfo = Consts.DirUserData + "ion.bin";
		private static readonly string PathHIDCache = Consts.DirUserData + "hid.bin";
		private static bool _ru = false;

		static IonApp()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			Directory.CreateDirectory(Consts.DirUserData);

#if DEBUG
			//File.Delete(PathActivationInfo);// dont remove-me from this #if #def
#endif

			if(File.Exists(PathHIDCache))
			{
				HID = File.ReadAllText(PathHIDCache);
				if(!string.IsNullOrWhiteSpace(HID))
					return;
			}


#if WINDOWS
			HID = HIDWindows.GetHID();
#elif GTKMONO
			HID = HIDLinux.GetHID();
#elif OSX
			var uuid = HIDOSX.MacUUID();
			var serial = HIDOSX.GetSerialNumber();
			HID = uuid + "-" + serial;
#endif

			File.WriteAllText(PathHIDCache, HID);
		}

		public static int ExpireDateEpoch
		{
			get => SimpleBSON.Load(File.ReadAllBytes(PathActivationInfo))["de"].int32Value;
		}
		public static DateTime ExpireDate
		{
			get => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ExpireDateEpoch);
		}
		public static string ActivationSignature
		{
			get => SimpleBSON.Load(File.ReadAllBytes(PathActivationInfo))["as"].stringValue;
		}
		public static bool IsTrial
		{
			get => SimpleBSON.Load(File.ReadAllBytes(PathActivationInfo))["tr"].boolValue;
		}
		public static int RemainingDays
		{
			get => (int)Math.Ceiling((ExpireDate - UpdateControl.GetDateTime()).TotalDays);
		}
		public static string LicensedName
		{
			get => SimpleBSON.Load(File.ReadAllBytes(PathActivationInfo))["name"].stringValue;
		}

		private static string HIDHash64 // hash only used for querying and ensure trust
		{
			get
			{
				byte[] key = BaseHost.LoadResource("ion_key.bin");
				using(var hmac = new HMACSHA512(key))
				{
					byte[] hid = Encoding.UTF8.GetBytes(HID);
					byte[] hash = hmac.ComputeHash(hid);
					return Convert.ToBase64String(hash);
				}
			}
		}

		private static byte[] ActivationHash // never save it!
		{
			get
			{
				byte[] key = BaseHost.LoadResource("ion_key.bin");
				using(var hmac = new HMACSHA512(key))
				{
					byte[] hid = Encoding.ASCII.GetBytes(HID);
					byte[] de = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ExpireDateEpoch));
					for(int i = 0; i < de.Length; i++)
						hid[i] |= de[i];
					return hmac.ComputeHash(hid);
				}
			}
		}

		public static bool VerifySignature(string signature64)
		{
			using(var rsa = new RSACryptoServiceProvider(2048))
			{
				rsa.FromXmlString(Encoding.UTF8.GetString(BaseHost.LoadResource("ion_rsa.txt")));

				var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
				rsaDeformatter.SetHashAlgorithm("SHA512");
				return rsaDeformatter.VerifySignature(ActivationHash, Convert.FromBase64String(signature64));
			}
		}

		public static EIonStatus GetStatus()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				if(!File.Exists(PathActivationInfo))
					return EIonStatus.INACTIVE;

				if(UpdateControl.GetDateTime() > ExpireDate)
					return EIonStatus.EXPIRED;

				bool sign = VerifySignature(ActivationSignature);
				if(!sign)
				{
					return EIonStatus.INACTIVE;
				}

#if !DEBUG
				if(sw.ElapsedMilliseconds > 3000)
				{
					return EIonStatus.INACTIVE;
				}
#endif

				RegisterUsage();
				return EIonStatus.ACTIVE;
			}
			catch(Exception ex)
			{
				return EIonStatus.INACTIVE;
			}
		}

		private static Tuple<EIonActivationResult, string> InternalActivate(string url)
		{
			using(var wc = new WebClient())
			{
				try
				{
					var res = wc.DownloadData(url);
					File.WriteAllBytes(PathActivationInfo, res);
					var status = GetStatus();
					if(status == EIonStatus.ACTIVE)
						return Tuple.Create(EIonActivationResult.SUCCESS, "");
					return Tuple.Create(EIonActivationResult.FAIL_ERROR, status.ToString());
				}
				catch(WebException ex)
				{
					var msg = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
					return Tuple.Create(EIonActivationResult.FAIL_SERVER_UNAUTHORIZED, "Server error message: " + msg);
				}
				catch(Exception ex)
				{
					return Tuple.Create(EIonActivationResult.FAIL_NO_INTERNET, "Please, check your internet connection.");
				}
			}
		}

		private static void RegisterUsage()
		{
			if(_ru)
				return;
			_ru = true;

			Task.Run(() =>
			{
				using(WebClient wb = new WebClient())
				{
					var url = IonConsts.URL_APPRU + $"?{URIAuthData}";
					wb.DownloadString(url);
				}
			});
		}

		public static Tuple<EIonActivationResult, string> ActivateTrial(string name, string email)
		{
			string hid = HID;
			string h64 = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(HID)));
			string hh64 = Uri.EscapeDataString(HIDHash64);
			name = Uri.EscapeDataString(name);
			email = Uri.EscapeDataString(email);
			return InternalActivate(IonConsts.URL_TRIAL + $"?ep={Consts.ProductID}&eos={IonConsts.OS}&h64={h64}&hh64={hh64}&name={name}&email={email}");
		}

		public static Tuple<EIonActivationResult, string> ActivateFully(string name, string email, string serial)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(name));
			Debug.Assert(!string.IsNullOrWhiteSpace(email));
			Debug.Assert(!string.IsNullOrWhiteSpace(serial));

			string hid = HID;
			string h64 = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(HID)));
			string hh64 = Uri.EscapeDataString(HIDHash64);
			name = Uri.EscapeDataString(name);
			email = Uri.EscapeDataString(email);
			return InternalActivate(IonConsts.URL_FULLY + $"?ep={Consts.ProductID}&eos={IonConsts.OS}&h64={h64}&hh64={hh64}&name={name}&email={email}&serial={serial}");
		}

		public static SciterValue GetUIExpando()
		{
			var status = GetStatus();

			SciterValue sv = new SciterValue();
			sv["active"] = new SciterValue(status == EIonStatus.ACTIVE);
			sv["status"] = new SciterValue(status.ToString());

			if(status == EIonStatus.ACTIVE || status == EIonStatus.EXPIRED)
				sv["is_trial"] = new SciterValue(IsTrial);
			if(status == EIonStatus.ACTIVE)
				sv["rd"] = new SciterValue(RemainingDays);
			sv["name"] = new SciterValue((args) => new SciterValue(LicensedName));

			sv["activate"] = new SciterValue((args) =>
			{
				Task.Run(() =>
				{
					Thread.Sleep(1000);
					Tuple<EIonActivationResult, string> res;
					if(args.Length==1)
						res = ActivateTrial("", "");
					else
						// name, e-mail, serial
						res = ActivateFully(args[1].Get(""), args[2].Get(""), args[3].Get(""));

					args[0].Call(new SciterValue(res.Item1 == EIonActivationResult.SUCCESS), new SciterValue(res.Item2));
				});
				return new SciterValue();
			});
			return sv;
		}
	}
}