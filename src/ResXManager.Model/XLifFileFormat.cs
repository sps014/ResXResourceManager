using System.Collections.Generic;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;

namespace ResXManager.Model.XLif
{
    [XmlRoot(ElementName = "group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Group
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }
        [XmlElement(ElementName = "group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public List<Group> Groups { get; set; } = new();
        [XmlElement(ElementName = "trans-unit", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public List<Transunit> Transunits = new();
    }

    [XmlRoot(ElementName = "target", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Target
    {
        [XmlAttribute(AttributeName = "state")]
        public string State { get; set; }
        [XmlText]
        public string Text { get; set; }
        [XmlAttribute(AttributeName = "state-qualifier")]
        public string Statequalifier { get; set; }
    }

    [XmlRoot(ElementName = "context", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Context
    {
        [XmlAttribute(AttributeName = "context-type")]
        public string Contexttype { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "context-group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Contextgroup
    {
        [XmlElement(ElementName = "context", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Context Context { get; set; }
    }

    [XmlRoot(ElementName = "trans-unit", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Transunit
    {
        [XmlElement(ElementName = "source", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public string Source { get; set; }
        [XmlElement(ElementName = "target", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Target Target { get; set; }
        [XmlElement(ElementName = "context-group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Contextgroup Contextgroup { get; set; }
        [XmlElement(ElementName = "rowstatus", Namespace = "http://www.sisulizer.com")]
        public string Rowstatus { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "resname")]
        public string Resname { get; set; }
        [XmlElement(ElementName = "invalidated", Namespace = "http://www.sisulizer.com")]
        public string Invalidated { get; set; }
        [XmlElement(ElementName = "note", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public string Note { get; set; }
        [XmlElement(ElementName = "comment", Namespace = "http://www.sisulizer.com")]
        public string Comment { get; set; }
    }

    [XmlRoot(ElementName = "body", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Body
    {
        [XmlElement(ElementName = "group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public List<Group> Group { get; set; } = new();
    }

    [XmlRoot(ElementName = "file", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class File
    {
        [XmlElement(ElementName = "body", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Body Body { get; set; }
        [XmlAttribute(AttributeName = "original")]
        public string Original { get; set; }
        [XmlAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }
        [XmlAttribute(AttributeName = "source-language")]
        public string Sourcelanguage { get; set; }
        [XmlAttribute(AttributeName = "target-language")]
        public string Targetlanguage { get; set; }
    }

    [XmlRoot(ElementName = "xliff", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Xliff
    {
        [XmlElement(ElementName = "file", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public File File { get; set; }
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string SchemaLocation { get; set; } = "urn:oasis:names:tc:xliff:document:1.2 xliff-core-1.2-transitional.xsd";
        [XmlAttribute(AttributeName = "sl", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Sl { get; set; } = "http://www.sisulizer.com";
    }

}