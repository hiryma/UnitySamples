using System.Text;
using System.Xml;

namespace Kayac
{
	static class HtmlUtil
	{
		public static XmlWriter CreateWriter(StringBuilder sb)
		{
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "\t";
			settings.NewLineChars = "\n";
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create(sb, settings);
			return writer;
		}

		public static void WriteHeader(XmlWriter writer, string title)
		{
			writer.WriteDocType("html", null, null, null);
			writer.WriteStartElement("html");
			writer.WriteStartElement("head");
			writer.WriteStartElement("meta");
			writer.WriteAttributeString("charset", "UTF-8");
			writer.WriteEndElement();
			writer.WriteElementString("title", title);
			writer.WriteEndElement(); // head
		}

		public static void WriteA(XmlWriter writer, string hrefAttribute, string innerText)
		{
			writer.WriteStartElement("a");
			writer.WriteAttributeString("href", hrefAttribute);
			writer.WriteString(innerText);
			writer.WriteEndElement();
		}

		public static void WriteTextarea(
			XmlWriter writer,
			string id,
			int rows = 0,
			int cols = 0,
			string value = "")
		{
			writer.WriteStartElement("textarea");
			writer.WriteAttributeString("id", id);
			if (rows > 0)
			{
				writer.WriteAttributeString("rows", rows.ToString());
			}
			if (cols > 0)
			{
				writer.WriteAttributeString("cols", cols.ToString());
			}
			writer.WriteString(value);
			writer.WriteEndElement(); // textarea
		}

		public static void WriteBr(XmlWriter writer)
		{
			writer.WriteStartElement("br");
			writer.WriteEndElement();
		}

		public static void WriteInput(
			XmlWriter writer,
			string id,
			string type,
			string value = "")
		{
			writer.WriteStartElement("input");
			writer.WriteAttributeString("id", id);
			writer.WriteAttributeString("type", type);
			writer.WriteAttributeString("value", value);
			writer.WriteEndElement();
		}

		public static void WriteOutput(
			XmlWriter writer,
			string id,
			string value = "")
		{
			writer.WriteStartElement("output");
			writer.WriteAttributeString("id", id);
			writer.WriteAttributeString("value", value);
			writer.WriteEndElement();
		}
	}
}