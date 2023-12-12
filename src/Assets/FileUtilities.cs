using System.IO;

namespace Receiver2ModdingKit.Assets {
	public static class FileUtilities {
		public static bool CanRead(string file_name) {
			try {
				using (var stream = File.OpenRead(file_name)) {
					return true;
				}
			} catch (IOException) {
				return false;
			}
		}

		public static bool CanWrite(string file_name) {
			try {
				using (var stream = File.OpenWrite(file_name)) {
					return true;
				}
			} catch (IOException) {
				return false;
			}
		}

		public static bool CanRead(this FileInfo file) {
			try {
				using (var stream = file.OpenRead()) {
					return true;
				}
			} catch (IOException) {
				return false;
			}
		}

		public static bool CanWrite(this FileInfo file) {
			try {
				using (var stream = file.OpenWrite()) {
					return true;
				}
			} catch (IOException) {
				return false;
			}
		}		
	}
}
