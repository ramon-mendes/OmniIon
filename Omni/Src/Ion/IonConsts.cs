using System;

namespace Ion
{
	static class IonConsts
	{
		public const string SERVER = "http://ion-mvc.azurewebsites.net/";
		//public const string SERVER = "http://localhost:53470/";
		public const string URL_TRIAL = SERVER + "Ion/ActivateTrial";
		public const string URL_TRIAL_NAMED = SERVER + "Ion/ActivateTrialNamed";
		public const string URL_FULLY = SERVER + "Ion/ActivateFully";
		public const string URL_APPINFO = SERVER + "Info/PI";
		public const string URL_APPRU = SERVER + "Info/RU";

#if WINDOWS || MVC
		public const EOS OS = EOS.WINDOWS;
#elif OSX
		public const EOS OS = EOS.MAC;
#elif GTKMONO
		public const EOS OS = EOS.LINUX;
#else
		#error Hey
#endif

		public static readonly int[] XOR =
		{
			0,			// INVALID
			86986903,	// TEST_DUCK
			13602333,	// FONTDROP
			99925180,	// ICONDROP
			29821653,	// PATTERNDROP
			63799142,	// ATCOMMANDER
			11104578,	// OMNI
			55214551,	// OMNICODE
			89245354,	// DESIGNARSENAL
			0,			// OMNILITE
			0,			// STICKY_NOTES
		};
	}


	public enum EProduct
	{
		INVALID,
		TEST_DUCK,
		FONTDROP,
		ICONDROP,
		PATTERNDROP,
		ATCOMMANDER,
		OMNI,
		OMNICODE,
		DESIGNARSENAL,
		OMNILITE,
		STICKY_NOTES,
	}

	public enum EOS
	{
		INVALID,
		WINDOWS,
		MAC,
		LINUX
	}

	public enum EIonStatus
	{
		INACTIVE,
		EXPIRED,
		ACTIVE
	}

	public enum EIonActivationResult
	{
		FAIL_ERROR,
		FAIL_NO_INTERNET,
		FAIL_SERVER_UNAUTHORIZED,
		SUCCESS,
	}
}