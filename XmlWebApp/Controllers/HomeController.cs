using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using XmlWebApp.Models;
using System.Xml.Serialization;
using System.IO;

/*
* !!! Important
* Iterate XML nodes without reading whole file. 
*/

namespace XmlWebApp.Controllers {
	public class HomeController : Controller {
		enum UploadType {
			xmlNodes,
			xmlSerializer,
			LinqToXml
		}

		public ActionResult GetBooks() {
			var data = new List<Book>();

			data = ReturnData();

			ViewBag.Books = data;

			return View("Index");
		}

		[HttpPost]
		public ActionResult Upload() {
			if (Request != null) {
				var uploadType = (UploadType)Convert.ToInt32(Request.Params[0]);

				switch (uploadType) {
					case UploadType.xmlNodes:
						UploadViaXmlNodes(Request);
						break;
					case UploadType.xmlSerializer:
						UploadViaXmlSerializer(Request);
						break;
				}
			}

			return GetBooks();
		}

		private void UploadViaXmlSerializer(HttpRequestBase request) {
			for (int i = 0; i < request.Files.Count; i++) {
				HttpPostedFileBase fileData = request.Files[i];
				Books bookList = null;

				//Serializable example
				XmlSerializer serializer = new XmlSerializer(typeof(Books));
				using (StreamReader sr = new StreamReader(fileData.InputStream)) {
					bookList = (Books)(serializer.Deserialize(sr));
				}

				foreach (Book book in bookList.Book) {
					AddNewBookViaXmlNodes(book);
				}
			}
		}

			private void UploadViaXmlNodes(HttpRequestBase request) {
			for (int i = 0; i < request.Files.Count; i++) {
				HttpPostedFileBase fileData = request.Files[i];

				if (fileData.ContentType == Consts.xmlType) {
					UploadXmlFile(fileData);
				}
				if (fileData.ContentType == Consts.xlsxType) {
					UploadXlsxFile(fileData);
				}
			}
		}

		private void UploadXmlFile(HttpPostedFileBase fileData) {
			var xDoc = new XmlDocument();
			fileData.InputStream.Position = 0;

			xDoc.Load(fileData.InputStream);

			XmlElement xRoot = xDoc.DocumentElement;
			
			foreach (XmlNode xnode in xRoot) {
				Book book = GetDataFromXml(xnode);
				AddNewBookViaXmlNodes(book);
			}
		}

		private void UploadXlsxFile(HttpPostedFileBase fileData) {
			if ((fileData != null) && (fileData.ContentLength > 0)) {
				using (var package = new ExcelPackage(fileData.InputStream)) {
					var currentSheet = package.Workbook.Worksheets;
					var workSheet = currentSheet.First();
					var numberOfRow = workSheet.Dimension.End.Row;

					for (int rowIterator = 2; rowIterator <= numberOfRow; rowIterator++) {
						var book = new Book();

						int numberOfColumn = 1;
						book.Name = workSheet.Cells[rowIterator, numberOfColumn++].Value.ToString();
						book.Author = workSheet.Cells[rowIterator, numberOfColumn++].Value.ToString();
						book.Price = Convert.ToInt32(workSheet.Cells[rowIterator, numberOfColumn++].Value.ToString());
						book.Id = Convert.ToInt32(workSheet.Cells[rowIterator, numberOfColumn++].Value.ToString());

						AddNewBookViaXmlNodes(book);
					}
				}
			}
		}

		private void ValidaionFile (XmlDocument xDoc) {
			string SchemaPath = Server.MapPath(Consts.bookListSchemaPath);

			xDoc.Schemas.Add("", SchemaPath);
			xDoc.Validate(ValidationEventHandler);
		}

		private static void ValidationEventHandler(object sender, ValidationEventArgs e) {
			XmlSeverityType type = XmlSeverityType.Warning;
			if (Enum.TryParse<XmlSeverityType>("Error", out type)) {
				if (e.Severity == XmlSeverityType.Error) throw new Exception(e.Message);
			}
		}

		public FileResult GetXlsxTemplate() {
			string file_path = Server.MapPath(Consts.xlsxTemplatePath);
			string fileType = "application/xlsx";

			return File(file_path, fileType, "Template.xlsx");
		}

		public FileResult GetXmlTemplate() {
			string file_path = Server.MapPath(Consts.xmlTemplatePath);
			string fileType = "application/xml";

			return File(file_path, fileType, "XmlTemplate.xml");
		}

		[HttpPost]
		public ActionResult AddBook(Book book) {
			var uploadType = (UploadType)Convert.ToInt32(Request.Params[0]);

			switch (uploadType) {
				case UploadType.xmlNodes:
					AddNewBookViaXmlNodes(book);
					break;
				case UploadType.LinqToXml:
					AddBookViaLinqToXml(book);
					break;
			}

			return GetBooks();
		}

		public void AddBookViaLinqToXml(Book book) {
			string filePath = Server.MapPath(Consts.bookListXmlPath);
			string schemaPath = Server.MapPath(Consts.bookListSchemaPath);

			XDocument doc = XDocument.Load(filePath);

			doc.Element("books").Element("bookCollection").Add(
				new XElement("book",
				new XElement("name", book.Name),
				new XElement("author", book.Author),
				new XElement("price", book.Price),
				new XElement("id", book.Id))
				);

			XmlSchemaSet schemaSet = new XmlSchemaSet();
			schemaSet.Add("", schemaPath);

			doc.Validate(schemaSet, ValidationEventHandler);
			doc.Save(filePath);
		}

		/* STRART OF THE EXAMPLE OF WORKING WITH XML NODES */

		public List<Book> ReturnData() {
			var booksList = new List<Book>();

			string xmlData = Server.MapPath(Consts.bookListXmlPath);

			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlElement xRoot = doc.DocumentElement;
			foreach (XmlNode node in xRoot.FirstChild) {
				Book book = GetDataFromXml(node);
				booksList.Add(book);
			}

			return booksList;
		}

		private Book GetDataFromXml(XmlNode nodes) {
			Book book = new Book();
			foreach (XmlNode childNode in nodes) {
				switch (childNode.Name) {
					case "id":
						book.Id = Convert.ToInt32(childNode.InnerText);
						break;
					case "name":
						book.Name = childNode.InnerText;
						break;
					case "author":
						book.Author = childNode.InnerText;
						break;
					case "price":
						book.Price = Convert.ToInt32(childNode.InnerText);
						break;
				}
			}

			return book;
		}

		public void AddNewBookViaXmlNodes(Book book) {
			string xmlData = Server.MapPath(Consts.bookListXmlPath);
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlNode xmlRoot = doc.DocumentElement.FirstChild;
			
			XmlElement bookElem = doc.CreateElement("book");
			XmlElement nameElem = doc.CreateElement("name");
			XmlElement authorElem = doc.CreateElement("author");
			XmlElement priceElem = doc.CreateElement("price");
			XmlElement idElem = doc.CreateElement("id");

			XmlText nameText = doc.CreateTextNode(book.Name);
			XmlText authorText = doc.CreateTextNode(book.Author);
			XmlText priceText = doc.CreateTextNode(book.Price.ToString());
			XmlText idText = doc.CreateTextNode(book.Id.ToString());

			nameElem.AppendChild(nameText);
			authorElem.AppendChild(authorText);
			priceElem.AppendChild(priceText);
			idElem.AppendChild(idText);

			bookElem.AppendChild(nameElem);
			bookElem.AppendChild(authorElem);
			bookElem.AppendChild(priceElem);
			bookElem.AppendChild(idElem);

			xmlRoot.AppendChild(bookElem);

			ValidaionFile(doc);

			doc.Save(xmlData);
		}

		[HttpGet]
		public ActionResult Delete(int id) {
			string xmlData = Server.MapPath(Consts.bookListXmlPath);
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);
			
			XmlNode root = doc.DocumentElement.FirstChild;
			XmlNode node = root.SelectSingleNode(
				String.Format("book[id='{0}']",
				id));
			XmlNode outer = node.ParentNode;
			outer.RemoveChild(node);
			doc.Save(xmlData);

			return GetBooks();
		}

		public ActionResult DeleteAll() {
			string xmlData = Server.MapPath(Consts.bookListXmlPath);
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlNode xmlRoot = doc.DocumentElement.FirstChild;
			xmlRoot.RemoveAll();
			doc.Save(xmlData);

			return GetBooks();
		}

		/* END OF THE EXAMPLE OF WORKING WITH XML NODES */
	}
}