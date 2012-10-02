using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Mihmerk.XmlConfig
{
    internal static class Utility
    {
        internal static byte[] StringToUTF8ByteArray(String xml)
        {
            return new UTF8Encoding().GetBytes(xml);
        }

        /// <summary>
        /// Creates the xml attribute object.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <returns></returns>
        internal static void AppendAttribute(this XmlNode node, String name, String value)
        {
            var xmlDoc = node.Coalesce(n => n.OwnerDocument);
            if (xmlDoc == null)
                throw new InvalidOperationException("XmlNode has no OwnerDocument");

            XmlAttribute attr = xmlDoc.CreateAttribute(name);
            attr.Value = value;
// ReSharper disable PossibleNullReferenceException
            node.Attributes.Append(attr);
// ReSharper restore PossibleNullReferenceException
        }

        internal static TOut Coalesce<TIn, TOut>(this TIn value, Expression<Func<TIn, TOut>> expression) where TOut : class
        {
            var result = DigIn(value, expression);
            return (TOut)result;
        }

        internal static object DigIn(object value, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                    return DigIn(value, ((LambdaExpression)expression).Body);
                case ExpressionType.Parameter:
                    return value;
                case ExpressionType.MemberAccess:
                    {
                        var ma = (MemberExpression)expression;
                        var inner = DigIn(value, ma.Expression);
                        if (inner == null)
                            return null;
                        if (ma.Member is PropertyInfo)
                        {
                            return ((PropertyInfo)ma.Member).GetGetMethod().Invoke(inner, null);
                        }
                        return ((FieldInfo)ma.Member).GetValue(inner);
                    }
                default:
                    throw new ArgumentException("Wrong lambda expression", "expression");
            }
        }
    }
}
