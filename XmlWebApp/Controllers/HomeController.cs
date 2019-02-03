using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Schema;
using XmlWebApp.Models;
using System.Xml.Serialization;
using System.IO;

/*
* Learn how to deserialize XML to Object. Object to .XML
* https://stackoverflow.com/questions/364253/how-to-des..
*
* Learn LINQ to XML
*
* !!! Important
* Iterate XML nodes without reading whole file. 
*/

namespace XmlWebApp.Controllers {
	public class HomeController : Controller {
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
			//Book book = new Book { Author = "Author 1", Name = "Book 1", Id = 1, Price = 500 };

			//XmlSerializer formatter = new XmlSerializer(typeof(Book));
			//string path = Server.MapPath("~/Files/test.xml");

			//using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate)) {
			//	formatter.Serialize(fs, book);
			//}

			for (int i = 0; i < request.Files.Count; i++) {
				HttpPostedFileBase fileData = request.Files[i];

				XmlSerializer serializer = new XmlSerializer(typeof(Book));
				using (StreamReader sr = new StreamReader(fileData.InputStream)) {
					Book book = (Book)(serializer.Deserialize(sr));
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
			ValidaionFile(fileData);

			var xDoc = new XmlDocument();
			fileData.InputStream.Position = 0;

			xDoc.Load(fileData.InputStream);

			XmlElement xRoot = xDoc.DocumentElement;
			
			foreach (XmlNode xnode in xRoot) {
				Book book = GetDataFromXml(xnode); // iterate xml nodes
				AddNewBook(book);
			}
		}

		private void ValidaionFile (HttpPostedFileBase fileData) {
			string SchemaPath = Server.MapPath("~/XML/BooksListSchema.xsd");

			var xDoc = new XmlDocument();
			xDoc.Load(fileData.InputStream);

			xDoc.Schemas.Add("", SchemaPath);
			xDoc.Validate(ValidationEventHandler);
		}

		private static void ValidationEventHandler(object sender, ValidationEventArgs e) {
			XmlSeverityType type = XmlSeverityType.Warning;
			if (Enum.TryParse<XmlSeverityType>("Error", out type)) {
				if (e.Severity == XmlSeverityType.Error) throw new Exception(e.Message);
			}
		}

		private void UploadXlsxFile (HttpPostedFileBase fileData) {
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

						AddNewBook(book);
					}
				}
			}
		}

		public FileResult GetXlsxTemplate() {
			string file_path = Server.MapPath("~/Files/Template.xlsx");
			string fileType = "application/xlsx";

			return File(file_path, fileType, "Template.xlsx");
		}

		public FileResult GetXmlTemplate() {
			string file_path = Server.MapPath("~/Files/XmlTemplate.xml");
			string fileType = "application/xml";

			return File(file_path, fileType, "XmlTemplate.xml");
		}

		/* STRART OF THE EXAMPLE OF WORKING WITH XML NODES */

		public List<Book> ReturnData() {
			var booksList = new List<Book>();

			string xmlData = Server.MapPath("~/XML/BooksList.xml");

			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlElement xRoot = doc.DocumentElement;
			foreach (XmlNode node in xRoot) {
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

		[HttpPost]
		public ActionResult AddBook(Book book) {
			AddNewBook(book);
			return GetBooks();
		}

		public void AddNewBook(Book book) {
			string xmlData = Server.MapPath("~/XML/BooksList.xml");
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlElement xmlRoot = doc.DocumentElement;

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
			doc.Save(xmlData);
		}

		[HttpGet]
		public ActionResult Delete(int id) {
			string xmlData = Server.MapPath("~/XML/BooksList.xml");
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);
			
			XmlNode root = doc.DocumentElement;
			XmlNode node = root.SelectSingleNode(
				String.Format("book[id='{0}']",
				id));
			XmlNode outer = node.ParentNode;
			outer.RemoveChild(node);
			doc.Save(xmlData);

			return GetBooks();
		}

		public ActionResult DeleteAll() {
			string xmlData = Server.MapPath("~/XML/BooksList.xml");
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlElement xmlRoot = doc.DocumentElement;
			xmlRoot.RemoveAll();
			doc.Save(xmlData);

			return GetBooks();
		}

		/* END OF THE EXAMPLE OF WORKING WITH XML NODES */
	}
}