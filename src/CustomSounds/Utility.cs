namespace Receiver2ModdingKit.CustomSounds {
	public static class Utility {
		/// <summary>
		/// Use this to break an 'if' chain if any operation returns an error, then print it
		/// </summary>
		/// <param name="result"> Result returned by an FMOD function call </param>
		/// <param name="errMessage"> Message to display in case of failure </param>
		/// <returns> False in case of success, true in case of an error </returns>
		public static bool IsError(FMOD.RESULT result, string errMessage = "") {
			if (result == FMOD.RESULT.OK) return false;
			
			if (errMessage != "") UnityEngine.Debug.LogError(errMessage + " => " + FMOD.Error.String(result));
			else UnityEngine.Debug.LogError(FMOD.Error.String(result));

			return true;
		}

		/// <summary>
		/// Use this in an if statement to check if a function returned OK
		/// </summary>
		/// <param name="result"> Result returned by an FMOD function call </param>
		/// <param name="errMessage"> Message to display in case of failure </param>
		/// <returns> True in case of success, false in case of an error </returns>
		public static bool CheckResult(FMOD.RESULT result, string errMessage = "") {
			return !IsError(result, errMessage);
		}
	}
}
