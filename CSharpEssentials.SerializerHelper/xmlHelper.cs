using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CSharpEssentials.SerializerHelper;
public class xmlHelper {
    private readonly XDocument _xmlDocument;
    Exception exception;
    public xmlHelper(string xmlContent) {
        _xmlDocument = XDocument.Parse(xmlContent);
    }
    public xmlHelper() {
    }
    public T DeserializeFromXml<T>(string xmlContent) {
        string xmlWithoutNamespaces = RemoveAllNamespaces(xmlContent); // da mettere su CSharpEssentials assieme al jsonhelper
        var serializer = new XmlSerializer(typeof(T));
        using (var stringReader = new StringReader(xmlWithoutNamespaces)) {
            return (T)serializer.Deserialize(stringReader);
        }
    }
    public static string SerializeXml(object obj) {
        XmlSerializer serializer = new XmlSerializer(obj.GetType());
        using (StringWriter writer = new StringWriter()) {
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }
    }
    /// <summary>
    /// Rimuove tutti i namespace da una stringa XML.
    /// </summary>
    private static string RemoveAllNamespaces(string xml) {
        XDocument doc = XDocument.Parse(xml);
        XElement newRoot = RemoveNamespaces(doc.Root);
        return newRoot.ToString();
    }
    /// <summary>
    /// Rimuove ricorsivamente i namespace da un XElement.
    /// </summary>
    private static XElement RemoveNamespaces(XElement element) {
        // Crea un nuovo elemento con lo stesso nome locale (senza namespace)
        XElement newElement = new XElement(element.Name.LocalName,
            // Aggiungi gli attributi, escludendo quelli di namespace
            element.Attributes()
                   .Where(a => !a.IsNamespaceDeclaration)
                   .Select(a => new XAttribute(a.Name.LocalName, a.Value)),
            // Applica ricorsivamente a tutti i figli
            element.Elements().Select(el => RemoveNamespaces(el))
        );
        // Se l'elemento non ha figli, imposta il suo valore
        if (!newElement.HasElements) {
            newElement.Value = element.Value;
        }
        return newElement;
    }
    public IEnumerable<XElement> GetValuesByTag(string tagName) {
        return _xmlDocument.Descendants(tagName)
                           .Select(element => element);
    }
    public string GetValueByAttribute(string AttributeName, string ValueonError) {
        try {
            //TODO: caricare tramite DI la classe request in modo da avere ovunque il refierimento con IdTransaction valorizzato!
            return _xmlDocument.Descendants("Column").Where(item => ((string)item?.Attribute("Name")).Equals(AttributeName, StringComparison.InvariantCultureIgnoreCase)).Attributes("Value").FirstOrDefault().Value;
        } catch (Exception ex){
            exception = ex;
            return ValueonError;
        }
    }
    public string GetSingleValue(string elementName, string ValueOnError) {
        try {
            return _xmlDocument.Descendants(elementName).FirstOrDefault()?.Value;
        } catch (Exception ex){
            exception = ex;
            return ValueOnError;
        }
    }
    public string GetAttributeByName(string tagName, string attributeName, string ValueOnError) {
        string result = "";
        try {
            result = _xmlDocument.Descendants(tagName).Attributes(attributeName).FirstOrDefault()?.Value;
        } catch (Exception ex){
            exception = ex;
            result = ValueOnError;
        }
        return result;
    }
}