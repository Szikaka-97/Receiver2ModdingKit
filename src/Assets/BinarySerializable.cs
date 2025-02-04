namespace Receiver2ModdingKit.Assets {
	public interface BinarySerializable {
		void Deserialize(FileEncoder encoder);
		FileEncoder Serialize(FileEncoder encoder);
		int GetSize();
	}
}