#if APP_MVC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IonMVC.Classes;

namespace Ion
{
	static class BaseHost
	{
		public static byte[] LoadResource(string path)
		{
			if(path == "ion_key.bin")
				return null;
			if(path == "ion_rsa.txt")
				return null;
			return null;
		}
	}

	static class Consts
	{
		public static readonly string DirUserData = "wtf";
		public const EProduct ProductID = EProduct.TEST_DUCK;
		public const int VersionInt = 1;
	}
}
#endif