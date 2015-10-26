//
// Authors:
// 	Duncan Mak  (duncan@ximian.com)
// 	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc. (http://www.ximian.com)
// Copyright (C) 2005-2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

/// <summary>
/// Requires a reference to System.Web.
/// </summary>
internal static partial class StringFormatWithExtension
{
	public static object Eval(object container, string expression)
	{
		expression = expression != null ? expression.Trim() : null;
		if (string.IsNullOrEmpty(expression))
		{
			throw new ArgumentNullException("expression");
		}
		var current = container;
		while (current != null)
		{
			var dot = expression.IndexOf('.');
			var size = (dot == -1) ? expression.Length : dot;
			var prop = expression.Substring(0, size);
			current = prop.IndexOf('[') == -1 ? GetPropertyValue(current, prop) : GetIndexedPropertyValue(current, prop);
			if (dot == -1)
			{
				break;
			}
			expression = expression.Substring(prop.Length + 1);
		}
		return current;
	}

	public static string Eval(object container, string expression, string format)
	{
		var result = Eval(container, expression);
		return FormatResult(result, format);
	}

	public static object GetIndexedPropertyValue(object container, string expr)
	{
		if (container == null)
		{
			throw new ArgumentNullException("container");
		}
		if ((expr == null) || (expr.Length == 0))
		{
			throw new ArgumentNullException("expr");
		}
		var openIdx = expr.IndexOf('[');
		var closeIdx = expr.IndexOf(']'); // see the test case. MS ignores all after the first ]
		if (openIdx < 0 || closeIdx < 0 || closeIdx - openIdx <= 1)
		{
			throw new ArgumentException(expr + " is not a valid indexed expression.");
		}
		var val = expr.Substring(openIdx + 1, closeIdx - openIdx - 1);
		val = val.Trim();
		if (val.Length == 0)
		{
			throw new ArgumentException(expr + " is not a valid indexed expression.");
		}
		var isString = false;
		// a quoted val means we have a string
		if ((val[0] == '\'' && val[val.Length - 1] == '\'') || (val[0] == '\"' && val[val.Length - 1] == '\"'))
		{
			isString = true;
			val = val.Substring(1, val.Length - 2);
		}
		else
		{
			// if all chars are digits, then we have a int
			foreach (char character in val)
			{
				if (!char.IsDigit(character))
				{
					isString = true;
					break;
				}
			}
		}
		var intVal = 0;
		if (!isString)
		{
			try
			{
				intVal = int.Parse(val);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(expr + " is not a valid indexed expression.", ex);
			}
		}
		string property;
		if (openIdx > 0)
		{
			property = expr.Substring(0, openIdx);
			if (!string.IsNullOrEmpty(property))
			{
				container = GetPropertyValue(container, property);
			}
		}
		if (container == null)
		{
			return null;
		}
		var list = container as IList;
		if (list != null)
		{
			if (isString)
			{
				throw new ArgumentException(expr + " cannot be indexed with a string.");
			}
			return list[intVal];
		}
		var t = container.GetType();
		// MS does not seem to look for any other than "Item"!!!
		var atts = t.GetCustomAttributes(typeof(DefaultMemberAttribute), false);
		property = atts.Length == 1 ? ((DefaultMemberAttribute) atts[0]).MemberName : "Item";
		var argTypes = new[] { (isString) ? typeof(string) : typeof(int) };
		var prop = t.GetProperty(property, argTypes);
		if (prop == null)
		{
			throw new ArgumentException(expr + " indexer not found.");
		}
		var args = new object[1];
		if (isString)
		{
			args[0] = val;
		}
		else
		{
			args[0] = intVal;
		}
		return prop.GetValue(container, args);
	}

	public static object GetPropertyValue(object container, string propName)
	{
		if (container == null)
		{
			throw new ArgumentNullException("container");
		}
		if (string.IsNullOrEmpty(propName))
		{
			throw new ArgumentNullException("propName");
		}
		var prop = TypeDescriptor.GetProperties(container).Find(propName, true);
		if (prop == null)
		{
			throw new FormatException("Property " + propName + " not found in " + container.GetType());
		}
		return prop.GetValue(container);
	}

	internal static string FormatResult(object result, string format)
	{
		if (result == null)
		{
			return string.Empty;
		}
		if (string.IsNullOrEmpty(format))
		{
			return result.ToString();
		}
		return string.Format(format, result);
	}
}