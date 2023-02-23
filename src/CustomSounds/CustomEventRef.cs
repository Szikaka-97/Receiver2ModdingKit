using System;

namespace Receiver2ModdingKit.CustomSounds {
	/// <summary>
	/// Signifies that a field is used as a custom sound event.
	/// Automatically binds a custom sound event with the same name as the field
	/// </summary>
	public class CustomEventRef : Attribute {
		CustomEventRef() { }
	}
}
