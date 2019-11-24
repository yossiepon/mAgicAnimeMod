//=========================================================================
///	<summary>
///		アセンブリヘルパ
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2019/11/24 新規作成</history>
//=========================================================================
using System;
using System.Reflection;

namespace Helpers
{
	static class AssemblyHelper
	{
		public static string AssemblyTitle
		{
			get
			{
				// このアセンブリ上のタイトル属性をすべて取得します
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				// 少なくとも 1 つのタイトル属性がある場合
				if (attributes.Length > 0)
				{
					// 最初の項目を選択します
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					// 空の文字列の場合、その項目を返します
					if (titleAttribute.Title != "")
						return titleAttribute.Title;
				}
				// タイトル属性がないか、またはタイトル属性が空の文字列の場合、.exe 名を返します
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

        public static string AssemblyVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}

        public static string AssemblyDescription
		{
			get
			{
				// このアセンブリ上の説明属性をすべて取得します
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				// 説明属性がない場合、空の文字列を返します
				if (attributes.Length == 0)
					return "";
				// 説明属性がある場合、その値を返します
				return ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

        public static string AssemblyProduct
		{
			get
			{
				// このアセンブリ上の製品属性をすべて取得します
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				// 製品属性がない場合、空の文字列を返します
				if (attributes.Length == 0)
					return "";
				// 製品属性がある場合、その値を返します
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

        public static string AssemblyCopyright
		{
			get
			{
				// このアセンブリ上の著作権属性をすべて取得します
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				// 著作権属性がない場合、空の文字列を返します
				if (attributes.Length == 0)
					return "";
				// 著作権属性がある場合、その値を返します
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

        public static string AssemblyCompany
		{
			get
			{
				// このアセンブリ上の会社属性をすべて取得します
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				// 会社属性がない場合、空の文字列を返します
				if (attributes.Length == 0)
					return "";
				// 会社属性がある場合、その値を返します
				return ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}
	}
}