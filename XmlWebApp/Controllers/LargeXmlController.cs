using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XmlWebApp.Models;
using OfficeOpenXml;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;

namespace XmlWebApp.Controllers
{
    public class LargeXmlController : Controller
    {
        public ActionResult GetBooks()
        {
			var data = new List<Book>();

			data = GetData();

			ViewBag.Books = data;

			return View("LargeXml");
		}

		public List<Book> GetData() {
			string filePath = Server.MapPath(Consts.bigDataListPath);

			List<Book> books = new List<Book>();
			XmlReader xml = XmlReader.Create(filePath);

			while (xml.Read()) {
				if (xml.NodeType == XmlNodeType.Element && xml.Name == "book") {
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(xml.ReadOuterXml());

					XmlNode node = xmlDoc.SelectSingleNode("book");

					books.Add(new Book {
						Name = node.SelectSingleNode("name").InnerText,
						Author = node.SelectSingleNode("author").InnerText,
						Price = Convert.ToInt32(node.SelectSingleNode("price").InnerText),
						Id = Convert.ToInt32(node.SelectSingleNode("id").InnerText)
					});

				}
			}

			xml.Close();

			return books;
		}

		public void CopyTo(XmlReader reader, XmlWriter writer, int id) {
			while (reader.Read()) {
				var currentElementName = "";
				while (reader.Read()) {
					//switch (reader.NodeType) {
					//	case XmlNodeType.Element:
					//		currentElementName = reader.Name;
					//		writer.WriteStartElement(reader.Name);

					//		//Copy all attributes verbatim
					//		if (reader.HasAttributes)
					//			writer.WriteAttributes(reader, true);

					//		//Handle empty elements by telling the writer to close right away
					//		if (reader.IsEmptyElement)
					//			writer.WriteEndElement();
					//		break;
					//	case XmlNodeType.EndElement:
					//		currentElementName = "";
					//		writer.WriteEndElement();
					//		break;
					//	case XmlNodeType.Text:
					//		if (reader.Value != id.ToString())
					//		writer.WriteString(reader.Value);
					//		break;
					//	case XmlNodeType.Whitespace:
					//		writer.WriteWhitespace(reader.Value);
					//		break;
					//		//Other cases. Attributes, comments etc.
					//}

					if (reader.NodeType == XmlNodeType.Element && reader.Name == "book") {
						XmlDocument xmlDoc = new XmlDocument();
						xmlDoc.LoadXml(reader.ReadOuterXml());

						XmlNode node = xmlDoc.SelectSingleNode("book");
						
						string identifier = node.SelectSingleNode("id").InnerText;

						if (identifier != id.ToString()) {
							writer.WriteNode(reader, true);
						}
					}

				}
			}
		}

		public ActionResult Delete(int id) {
			string filePath = Server.MapPath(Consts.bigDataListPath);

			var xmlReader = XmlReader.Create(System.IO.File.OpenRead(filePath));
			var xmlWriter = XmlWriter.Create(System.IO.File.OpenWrite(Server.MapPath("~/XML/NewLargeBooksList.xml")));


			CopyTo(xmlReader, xmlWriter, id);
			
			xmlReader.Close();
			xmlWriter.Close();

			return GetBooks();
		}

		public ActionResult DeleteAll() {
			string filePath = Server.MapPath(Consts.bigDataListPath);

			System.IO.File.Delete(filePath);

			XDocument doc = new XDocument(new XElement("books", new XElement("bookCollection")));
			doc.Save(filePath);

			return GetBooks();
		}

		public ActionResult GenerateFile(int nodesCount) {
			string filePath = Server.MapPath(Consts.bigDataListPath);
			XDocument xDocument = XDocument.Load(filePath);

			Random rand = new Random();
			for (int i = 1; i <= nodesCount; i++) {
				xDocument.Root.Element("bookCollection").Add(new XElement("book",
							new XElement("name", RandomString(rand, 10)),
							new XElement("author", RandomString(rand, 10)),
							new XElement("price", rand.Next(100, 10000)),
							new XElement("id", i)));
			}

			xDocument.Save(filePath);

			return GetBooks();
		}

		static string RandomString(Random random, int length) {
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}

	}
}