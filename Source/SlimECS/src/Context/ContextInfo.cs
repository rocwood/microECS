using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SlimECS
{
	public class ComponentTypeInfo
	{
		public readonly Type type;
		public readonly int index;
		public readonly bool zeroSize;

		public ComponentTypeInfo(Type type, int index, bool zeroSize)
		{
			this.type = type;
			this.index = index;
			this.zeroSize = zeroSize;
		}
	}

	public class ContextInfo
	{
		private static ComponentTypeInfo[] _componentInfoList;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComponentTypeInfo[] GetComponentInfoList()
		{
			if (_componentInfoList == null)
				_componentInfoList = CollectComponents();

			return _componentInfoList;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetIndexOf<T>() where T : struct, IComponent
		{
			var info = GetComponentInfo<T>();
			if (info != null)
				return info.index;

			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComponentTypeInfo GetComponentInfo<T>() where T : struct, IComponent
		{
			ref var info = ref ComponentTypeInfo<T>.info;
			if (info == null)
			{
				if (_componentInfoList == null)
					_componentInfoList = CollectComponents();

				info = Array.Find(_componentInfoList, x => x.type == typeof(T));
			}

			return info;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComponentTypeInfo[] CollectComponents()
		{
			var baseType = typeof(IComponent);

			// TODO: skip some types via Attribute
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.Where(s => !s.FullName.StartsWith("System.") && !s.FullName.StartsWith("SlimECS."))
				.SelectMany(s => s.GetTypes())
				.Where(t => t.IsValueType && !t.IsPrimitive && t.IsPublic && baseType.IsAssignableFrom(t))
				.ToArray();

			Array.Sort(types, (x, y) => string.CompareOrdinal(x.FullName, y.FullName));

			var list = new ComponentTypeInfo[types.Length];
			for (int i = 0; i < types.Length; i++)
			{
				var t = types[i];
				list[i] = new ComponentTypeInfo(t, i, IsZeroSizeStruct(t));
			}

			return list;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// https://stackoverflow.com/a/27851610
		private static bool IsZeroSizeStruct(Type t)
		{
			return t.IsValueType && !t.IsPrimitive 
				&& t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.All(field => IsZeroSizeStruct(field.FieldType));
		}

		class ComponentTypeInfo<T> where T : struct, IComponent
		{
			public static ComponentTypeInfo info;
		}
	}
}
