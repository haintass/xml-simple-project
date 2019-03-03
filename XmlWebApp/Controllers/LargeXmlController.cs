using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using XmlWebApp.Models;
using System.Xml;
using System.Xml.Linq;

using IOFile = System.IO.File;
using System.IO;
using System.Text.RegularExpressions;

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

		public List<Book> GetTextData() {
			string filePath = Server.MapPath(Consts.bigDataListPath);
			string newFilePath = Server.MapPath("~/XML/NewLargeBooksList.xml");

			List<Book> books = new List<Book>();

			using (StreamReader s = IOFile.OpenText(filePath)) {
				Regex regex = new Regex(@"<book[\s||>]");
				string str;
				while ((str = s.ReadLine()) != null) {
					if (regex.Match(str).Success) {
						string xml = str + ReadOuterXml(s);

						XmlDocument xmlDoc = new XmlDocument();
						xmlDoc.LoadXml(xml);

						XmlNode node = xmlDoc.SelectSingleNode("book");

						books.Add(new Book {
							Name = node.SelectSingleNode("name").InnerText,
							Author = node.SelectSingleNode("author").InnerText,
							Price = Convert.ToInt32(node.SelectSingleNode("price").InnerText),
							Id = Convert.ToInt32(node.SelectSingleNode("id").InnerText)
						});
					}
				}
			}

			return books;
		}

		private string ReadOuterXml(StreamReader s) {
			Regex regex = new Regex(@"</book>");
			string outerXml = string.Empty;

			string str;
			while (!regex.Match(str = s.ReadLine()).Success) {
				outerXml += str;
			}
			outerXml += str;

			return outerXml;
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

		public ActionResult Delete(int id) {
			string filePath = Server.MapPath(Consts.bigDataListPath);
			string newFilePath = Server.MapPath("~/XML/template.xml");

			using (XmlReader xml = XmlReader.Create(filePath)) {
				using (XmlWriter writer = XmlWriter.Create(newFilePath)) {
					writer.WriteStartDocument();
					writer.WriteWhitespace("\n");

					writer.WriteStartElement("books");
					writer.WriteWhitespace("\n");

					writer.WriteWhitespace("  ");
					writer.WriteStartElement("bookCollection");
					writer.WriteWhitespace("\n");

					while (xml.Read()) {
						if (xml.NodeType == XmlNodeType.Element && xml.Name == "book") {
							XmlDocument xmlDoc = new XmlDocument();
							xmlDoc.LoadXml(xml.ReadOuterXml());

							XmlNode node = xmlDoc.SelectSingleNode("book");

							if (Convert.ToInt32(node.SelectSingleNode("id").InnerText) != id) {
								writer.WriteWhitespace("    ");
								writer.WriteStartElement("book");
								writer.WriteAttributeString("language", node.Attributes.GetNamedItem("language").Value);
								writer.WriteWhitespace("\n");

								writer.WriteWhitespace("      ");
								writer.WriteElementString("name", node.SelectSingleNode("name").InnerXml);
								writer.WriteWhitespace("\n");

								writer.WriteWhitespace("      ");
								writer.WriteElementString("author", node.SelectSingleNode("author").InnerXml);
								writer.WriteWhitespace("\n");

								writer.WriteWhitespace("      ");
								writer.WriteElementString("price", node.SelectSingleNode("price").InnerXml);
								writer.WriteWhitespace("\n");

								writer.WriteWhitespace("      ");
								writer.WriteElementString("id", node.SelectSingleNode("id").InnerXml);
								writer.WriteWhitespace("\n");

								writer.WriteWhitespace("    ");
								writer.WriteEndElement();
								writer.WriteWhitespace("\n");
							}
						}
					}

					writer.WriteWhitespace("  ");
					writer.WriteEndElement();
					writer.WriteWhitespace("\n");

					writer.WriteEndElement();
					writer.WriteWhitespace("\n");

					writer.WriteEndDocument();
				}
			}
			
			IOFile.Delete(filePath);
			IOFile.Move(newFilePath, filePath);

			return GetBooks();
		}

		public ActionResult DeleteAll() {
			string filePath = Server.MapPath(Consts.bigDataListPath);

			IOFile.Delete(filePath);

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
							new XAttribute("language", rand.Next(1, 100) % 2 == 0 ? "English" : "Russian"),
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