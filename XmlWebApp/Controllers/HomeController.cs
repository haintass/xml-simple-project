using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using XmlWebApp.Models;

namespace XmlWebApp.Controllers {
	public class HomeController : Controller {
		public ActionResult Index() {
			return GetBooks();
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
				for (int i = 0; i < Request.Files.Count; i++) {
					HttpPostedFileBase fileData = Request.Files[i];
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

								AddBook(book);
							}
						}
					}
				}
			}

			return GetBooks();
		}

		public FileResult GetTemplate() {
			string file_path = Server.MapPath("~/Files/Template.xlsx");
			string fileType = "application/xlsx";

			return File(file_path, fileType, "Template.xlsx");
		}

		[HttpPost]
		public ActionResult AddBook(Book book) {
			AddNewBook(book);
			return GetBooks();
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

		public List<Book> ReturnData() {
			var booksList = new List<Book>();

			string xmlData = Server.MapPath("~/XML/BooksList.xml");
			
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlData);

			XmlElement xRoot = doc.DocumentElement;
			foreach(XmlNode node in xRoot) {
				Book book = new Book();
				foreach (XmlNode childNode in node.ChildNodes) {
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
				booksList.Add(book);
			}

			return booksList;
		}
	}
}