using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ion;

namespace Omni
{
	static partial class Consts
	{
		public static readonly string DirUserData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/MISoftware/Omni/";

		public const EProduct ProductID = EProduct.OMNI;
		public const string AppName = "Omni";
	}
}