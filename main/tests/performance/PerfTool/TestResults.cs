using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml;

namespace PerfTool.TestModel
{
	[XmlRoot (ElementName = "environment")]
	public class Environment
	{
		[XmlAttribute (AttributeName = "macunit-version")]
		public string MacunitVersion { get; set; }

		[XmlAttribute (AttributeName = "clr-version")]
		public string ClrVersion { get; set; }

		[XmlAttribute (AttributeName = "os-version")]
		public string OSVersion { get; set; }

		[XmlAttribute (AttributeName = "platform")]
		public string Platform { get; set; }

		[XmlAttribute (AttributeName = "cwd")]
		public string Cwd { get; set; }

		[XmlAttribute (AttributeName = "machine-name")]
		public string MachineName { get; set; }

		[XmlAttribute (AttributeName = "user")]
		public string User { get; set; }

		[XmlAttribute (AttributeName = "user-domain")]
		public string UserDomain { get; set; }
	}

	[XmlRoot (ElementName = "culture-info")]
	public class CultureInfo
	{
		[XmlAttribute (AttributeName = "current-culture")]
		public string CurrentCulture { get; set; }

		[XmlAttribute (AttributeName = "current-uiculture")]
		public string CurrentUICulture { get; set; }
	}

	[XmlRoot (ElementName = "property")]
	[XmlType ("property")]
	public class Property
	{
		[XmlAttribute (AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute (AttributeName = "value")]
		public string Value { get; set; }
	}

	[XmlRoot (ElementName = "reason")]
	public class Reason
	{
		[XmlIgnore]
		public string Message { get; set; }

		[XmlElement ("message")]
		public XmlCDataSection CDataMessage {
			get {
				XmlDocument doc = new XmlDocument ();
				return doc.CreateCDataSection (Message);
			}
			set {
				Message = value.Value;
			}
		}
	}

	[XmlRoot (ElementName = "failure")]
	public class Failure
	{
		[XmlIgnore]
		public string Message { get; set; }

		[XmlElement ("message")]
		public XmlCDataSection CDataMessage {
			get {
				XmlDocument doc = new XmlDocument ();
				return doc.CreateCDataSection (Message);
			}
			set {
				Message = value.Value;
			}
		}

		[XmlIgnore]
		public string StackTrace { get; set; }

		[XmlElement ("stack-trace")]
		public XmlCDataSection CDataStackTrace {
			get {
				XmlDocument doc = new XmlDocument ();
				return doc.CreateCDataSection (StackTrace);
			}
			set {
				StackTrace = value.Value;
			}
		}
	}

	[XmlRoot (ElementName = "improvement")]
	public class Improvement
	{
		[XmlIgnore]
		public string Message { get; set; }

		[XmlElement ("message")]
		public XmlCDataSection CDataMessage {
			get {
				XmlDocument doc = new XmlDocument ();
				return doc.CreateCDataSection (Message);
			}
			set {
				Message = value.Value;
			}
		}

		[XmlAttribute (AttributeName = "time")]
		public double Time { get; set; }

		[XmlAttribute (AttributeName = "old-time")]
		public double OldTime { get; set; }

		[XmlAttribute (AttributeName = "delta")]
		public double Delta {
			get {
				return OldTime - Time;
			}
			set {
				// Ignore
			}
		}
	}

	[XmlRoot (ElementName = "test-case")]
	public class TestCase
	{
		[XmlAttribute (AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute (AttributeName = "executed")]
		public string ExecutedString { get; set; }

		[XmlIgnore]
		public bool Executed => ExecutedString == "True";

		[XmlAttribute (AttributeName = "result")]
		public string Result { get; set; }

		[XmlAttribute (AttributeName = "success")]
		public string SuccessString { get; set; }

		[XmlIgnore]
		public bool Success {
			get { return SuccessString == "True"; }
			set {
				if (value)
					SuccessString = "True";
				else
					SuccessString = "False";
			}
		}

		[XmlAttribute (AttributeName = "time")]
		public double Time { get; set; }

		[XmlAttribute (AttributeName = "asserts")]
		public int Asserts { get; set; }

		[XmlArray (ElementName = "properties")]
		public List<Property> Properties { get; set; }

		[XmlElement (ElementName = "reason")]
		public Reason Reason { get; set; }

		[XmlElement (ElementName = "failure")]
		public Failure Failure { get; set; }

		[XmlElement (ElementName = "improvement")]
		public Improvement Improvement { get; set; }
	}

	[XmlRoot (ElementName = "results")]
	public class Results
	{
		[XmlElement (ElementName = "test-case")]
		public List<TestCase> TestCases { get; set; }

		[XmlElement (ElementName = "test-suite")]
		public List<TestSuite> TestSuites { get; set; }
	}

	[XmlRoot (ElementName = "test-suite")]
	public class TestSuite
	{
		[XmlElement (ElementName = "results")]
		public Results Results { get; set; }

		[XmlAttribute (AttributeName = "type")]
		public string Type { get; set; }

		[XmlAttribute (AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute (AttributeName = "executed")]
		public string ExecutedString { get; set; }

		[XmlIgnore]
		public bool Executed => ExecutedString == "True";

		[XmlAttribute (AttributeName = "result")]
		public string Result { get; set; }

		[XmlAttribute (AttributeName = "success")]
		public string SuccessString { get; set; }

		[XmlIgnore]
		public bool Success {
			get { return SuccessString == "True"; }
			set {
				if (value)
					SuccessString = "True";
				else
					SuccessString = "False";
			}
		}

		[XmlAttribute (AttributeName = "time")]
		public double Time { get; set; }

		[XmlAttribute (AttributeName = "asserts")]
		public int Asserts { get; set; }

		[XmlElement (ElementName = "reason")]
		public Reason Reason { get; set; }

		[XmlElement (ElementName = "failure")]
		public Failure Failure { get; set; }
	}

	[XmlRoot (ElementName = "test-results")]
	public class TestResults
	{
		[XmlElement (ElementName = "environment")]
		public Environment Environment { get; set; }

		[XmlElement (ElementName = "culture-info")]
		public CultureInfo Cultureinfo { get; set; }

		[XmlElement (ElementName = "test-suite")]
		public TestSuite TestSuite { get; set; }

		[XmlAttribute (AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute (AttributeName = "total")]
		public int Total { get; set; }

		[XmlAttribute (AttributeName = "errors")]
		public int Errors { get; set; }

		[XmlAttribute (AttributeName = "failures")]
		public int Failures { get; set; }

		[XmlAttribute (AttributeName = "not-run")]
		public int NotRun { get; set; }

		[XmlAttribute (AttributeName = "inconclusive")]
		public int Inconclusive { get; set; }

		[XmlAttribute (AttributeName = "ignored")]
		public int Ignored { get; set; }

		[XmlAttribute (AttributeName = "skipped")]
		public int Skipped { get; set; }

		[XmlAttribute (AttributeName = "invalid")]
		public int Invalid { get; set; }

		[XmlAttribute (AttributeName = "date")]
		public DateTime Date { get; set; }

		[XmlAttribute (AttributeName = "time")]
		public DateTime Time { get; set; }
	}
}
