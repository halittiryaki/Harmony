﻿using System;
using System.Globalization;
using System.Reflection;

namespace Harmony
{
	public class Traverse
	{
		private AccessCache Cache = new AccessCache();

		private Type _type;
		private object _root;
		private MemberInfo _info;
		private object[] _index;

		public static Traverse Create(Type type)
		{
			return new Traverse(type);
		}

		public static Traverse Create<T>()
		{
			return Create(typeof(T));
		}

		public static Traverse Create(object root)
		{
			return new Traverse(root);
		}

		private Traverse()
		{
		}

		public Traverse(Type type)
		{
			_type = type;
		}

		public Traverse(object root)
		{
			_root = root;
			_type = root == null ? null : root.GetType();
		}

		private Traverse(object root, MemberInfo info, object[] index)
		{
			_root = root;
			_type = root == null ? null : root.GetType();
			_info = info;
			_index = index;
		}

		public object GetValue()
		{
			if (_info is FieldInfo)
				return ((FieldInfo)_info).GetValue(_root);
			if (_info is PropertyInfo)
				return ((PropertyInfo)_info).GetValue(_root, AccessTools.all, null, _index, CultureInfo.CurrentCulture);
			if (_root == null && _type != null) return _type;
			return _root;
		}

		private Traverse Resolve()
		{
			if (_root == null && _type != null) return this;
			return new Traverse(GetValue());
		}

		public T GetValue<T>()
		{
			var value = GetValue();
			if (value == null) return default(T);
			return (T)value;
		}

		public Traverse SetValue(object value)
		{
			if (_info is FieldInfo)
				((FieldInfo)_info).SetValue(_root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
			if (_info is PropertyInfo)
				((PropertyInfo)_info).SetValue(_root, value, AccessTools.all, null, _index, CultureInfo.CurrentCulture);
			return this;
		}

		public Traverse Type(string name)
		{
			if (_type == null) return new Traverse();
			var type = AccessTools.Inner(_type, name);
			return new Traverse(type);
		}

		public Traverse Field(string name)
		{
			var resolved = Resolve();
			if (resolved._type == null) return new Traverse();
			var info = Cache.GetFieldInfo(resolved._type, name);
			if (info.IsStatic == false && resolved._root == null) return new Traverse();
			return new Traverse(resolved._root, info, null);
		}

		public Traverse Property(string name, object[] index = null)
		{
			var resolved = Resolve();
			if (resolved._root == null || resolved._type == null) return new Traverse();
			var info = Cache.GetPropertyInfo(_type, name);
			return new Traverse(resolved._root, info, index);
		}

		public Traverse Method(string name, params object[] arguments)
		{
			var resolved = Resolve();
			if (resolved._type == null) return new Traverse();
			var types = AccessTools.GetTypes(arguments);
			var info = Cache.GetMethodInfo(resolved._type, name, types);
			if (info == null) throw new MissingMethodException(name + types.Description());
			var val = info.Invoke(resolved._root, arguments);
			return new Traverse(val);
		}

		public Traverse Method(string name, Type[] paramTypes, object[] parameter)
		{
			var resolved = Resolve();
			if (resolved._type == null) return new Traverse();
			var info = Cache.GetMethodInfo(resolved._type, name, paramTypes);
			if (info == null) throw new MissingMethodException(name + paramTypes.Description());
			var val = info.Invoke(resolved._root, parameter);
			return new Traverse(val);
		}

		public override string ToString()
		{
			var value = GetValue();
			if (value == null) return null;
			return value.ToString();
		}
	}
}