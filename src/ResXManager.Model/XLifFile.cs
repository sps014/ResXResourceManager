using System.Collections.Generic;
using System.Xml.Serialization;

namespace ResXManager.Model.XLif
{
    [XmlRoot(ElementName = "target")]
    public class Target
    {

        [XmlAttribute(AttributeName = "state")]
        public string State { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "trans-unit")]
    public class Transunit
    {

        [XmlElement(ElementName = "source")]
        public string Source { get; set; }

        [XmlElement(ElementName = "target")]
        public Target Target { get; set; }

        [XmlElement(ElementName = "rowstatus")]
        public string Rowstatus { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "group")]
    public class Group
    {

        [XmlElement(ElementName = "transunit")]
        public List<Transunit> Transunit { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }

        [XmlText]
        public string Text { get; set; }

        [XmlElement(ElementName = "group")]
        public List<Group> Groups { get; set; }
    }

    [XmlRoot(ElementName = "body")]
    public class Body
    {

        [XmlElement(ElementName = "group")]
        public List<Group> Group { get; set; }
    }

    [XmlRoot(ElementName = "file")]
    public class File
    {

        [XmlElement(ElementName = "body")]
        public Body Body { get; set; }

        [XmlAttribute(AttributeName = "original")]
        public string Original { get; set; }

        [XmlAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }

        [XmlAttribute(AttributeName = "source-language")]
        public string SourceLanguage { get; set; }

        [XmlAttribute(AttributeName = "target-language")]
        public string TargetLanguage { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "xliff")]
    public class XliffFile
    {

        [XmlElement(ElementName = "file")]
        public File File { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; } = "1.2";

        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; } = "urn:oasis:names:tc:xliff:document:1.2";

        [XmlAttribute(AttributeName = "xsi")]
        public string Xsi { get; set; } = "http://www.w3.org/2001/XMLSchema-instance";

        [XmlAttribute(AttributeName = "schemaLocation")]
        public string SchemaLocation { get; set; } = "urn:oasis:names:tc:xliff:document:1.2 xliff-core-1.2-transitional.xsd";

        [XmlAttribute(AttributeName = "sl")]
        public string Sl { get; set; } = "http://www.sisulizer.com";

        [XmlText]
        public string Text { get; set; }
    }
}