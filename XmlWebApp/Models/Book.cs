using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace XmlWebApp.Models {
	[Serializable]
	public class Book {		
		[XmlElement(ElementName ="name")]
		public string Name { get; set; }

		[XmlElement("author")]
		public string Author { get; set; }

		[XmlElement("price")]
		public int Price { get; set; }

		[XmlElement("id")]
		public int Id { get; set; }
	}

	[Serializable]
	[XmlRoot("books")]
	public class Books{
		[XmlArray("bookCollection"), XmlArrayItem("book", typeof(Book))]
		public Book[] Book { get; set; }
	}
}