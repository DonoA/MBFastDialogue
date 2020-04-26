using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace MBFastDialogue
{
	public class Settings
	{
		[XmlElement("pattern_whitelist")]
		public Whitelist whitelist { get; set; } = new Whitelist();

		[XmlElement]
		public string toggle_key { get; set; } = "X";
	}

	public class Whitelist
	{
		[XmlElement("pattern")]
		public List<string> whitelistPatterns { get; set; } = new List<string>();
	}
}
