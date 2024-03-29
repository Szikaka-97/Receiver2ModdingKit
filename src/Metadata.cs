using UnityEngine;

namespace Receiver2ModdingKit
{
	public class Metadata<T> : Component
	{
		public string Name
		{
			get;
			private set;
		}
		public T Value
		{
			get;
			private set;
		}

		public void Set(T value)
		{
			Set(this.Name, value);
		}

		public void Set(string name, T value)
		{
			this.Name = name;
			this.Value = value;
		}
	}

	public static class MetadataExtensions
	{
		public static Metadata<T> AddMeta<T>(this GameObject gobj, string name, T value)
		{
			foreach (var meta in gobj.GetComponents<Metadata<T>>())
			{
				if (meta.Name == name)
				{
					meta.Set(value);

					return meta;
				}
			}

			Metadata<T> metaData = gobj.AddComponent<Metadata<T>>();

			metaData.Set(name, value);

			return metaData;
		}

		public static Metadata<T> SetMeta<T>(this GameObject gobj, string name, T value)
		{
			foreach (var meta in gobj.GetComponents<Metadata<T>>())
			{
				if (meta.Name == name)
				{
					meta.Set(value);

					return meta;
				}
			}

			return gobj.AddMeta(name, value);
		}

		public static Metadata<T> SetMeta<T>(this Component comp, string name, T value)
		{
			foreach (var meta in comp.GetComponents<Metadata<T>>())
			{
				if (meta.Name == name)
				{
					meta.Set(value);

					return meta;
				}
			}

			return comp.gameObject.AddMeta(name, value);
		}

		public static T GetMeta<T>(this GameObject gobj, string name)
		{
			foreach (var meta in gobj.GetComponents<Metadata<T>>())
			{
				if (meta.Name == name)
				{
					return meta.Value;
				}
			}

			return default(T);
		}

		public static T GetMeta<T>(this Component comp, string name)
		{
			foreach (var meta in comp.GetComponents<Metadata<T>>())
			{
				if (meta.Name == name)
				{
					return meta.Value;
				}
			}

			return default(T);
		}
	}
}
