using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Mihmerk.XmlConfig.Storage
{
    public class WrongXmlSettingsFileVersionArgs : WrongSettingsFileVersionArgs
    {
        public readonly XmlDocument Document;

        public WrongXmlSettingsFileVersionArgs(XmlDocument document, Version currentVersion, Version fileVersion)
            : base(currentVersion, fileVersion)
        {
            Document = document;
        }
    }

    internal abstract class XmlSettingsStorage : ISettingsStorage
    {
        private readonly EventWaitHandle _handle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private const int waitTimeout = 3000;

        public Func<WrongSettingsFileVersionArgs, object> SettingsNeedUpdateAction
        {
            get;
            set;
        }

        public void Load(Settings settings)
        {            
            if (!_handle.WaitOne(waitTimeout))
            {
                throw new TimeoutException("Cannot load settings because they are hold by another thread");    
            }
            _handle.Reset();

            try
            {
                XmlDocument xmlDocument = LoadXmlDocument();

                if (xmlDocument == null || xmlDocument.DocumentElement == null || !xmlDocument.DocumentElement.HasChildNodes)
                    return;

                //get file version
                Version fileVersion;
                if (Version.TryParse(xmlDocument.DocumentElement.Attributes["version"].Value, out fileVersion))
                {                    
#if DEBUG
                    if (fileVersion.ToString(4) == "0.0.0.0")
                    {
                        fileVersion = Settings.CurrentVersion;
                    }
#endif
                    if (fileVersion != Settings.CurrentVersion)
                    {
                        if (SettingsNeedUpdateAction != null)
                        {
                            xmlDocument = (XmlDocument)
                                          SettingsNeedUpdateAction(new WrongXmlSettingsFileVersionArgs(xmlDocument,
                                                                                                       Settings.
                                                                                                           CurrentVersion,
                                                                                                       fileVersion));
                            if (xmlDocument == null)
                            {
                                throw new InvalidOperationException("Cannot load settings, because they were not updated properly");
                            }
                        }
                        else
                        {
                            throw new NotSupportedException(String.Format("The settings file version is not supported: {0}. Should be: {1}", fileVersion, Settings.CurrentVersion));
                        }
                    }
                    settings.SettingsVersion = fileVersion;
                }

                //save not used settings               
                settings.NotUsedSettings = GetNotUsedNodes(xmlDocument, settings);

                //Load values to properties   
                foreach (var pair in settings.Properties)
                {
                    PropertyInfo prop = pair.Value;
                    var attr = (SettingItem)Attribute.GetCustomAttribute(prop, typeof(SettingItem));

                    var node = xmlDocument.DocumentElement.SelectSingleNode("descendant::item[@key='" + attr.Key + "']");
                    if (node != null)
                    {
                        if (((XmlElement)node).GetAttribute("IsNull") == "true")
                        {
                            settings[attr.Key] = null;
                        }
                        else
                        {
                            Object val = DeserializeObject(settings, prop.PropertyType, node.InnerXml, attr.DeserializerMethodName);
                            settings[attr.Key] = val;
                        }
                    }
                    else
                    {
                        //check for default value
                        var defAttr = (DefaultValueAttribute)Attribute.GetCustomAttribute(prop, typeof(DefaultValueAttribute));
                        if (defAttr != null)
                        {
                            settings[attr.Key] = defAttr.Value;
                        }
                    }
                }
            }
            finally
            {
                _handle.Set();
            }
        }

        public void Save(Settings settings)
        {
            if (!_handle.WaitOne(waitTimeout))
            {
                throw new TimeoutException("Cannot load settings because they are hold by another thread");
            }
            _handle.Reset();

            try
            {
                var xmlDocument = PrepareXmlDocForSave(settings);
                CommitChanges(xmlDocument);
            }
            finally
            {
                _handle.Set();
            }

        }

        protected abstract void CommitChanges(XmlDocument xmlDocument);
        public abstract XmlDocument LoadXmlDocument();
        protected abstract bool Exists { get; }

        private XmlDocument PrepareXmlDocForSave(Settings settings)
        {
            //Save values that are not used in this class to the array
            List<XmlNode> notUsedNodes;
            XmlDocument xmlDocument;
            if (Exists)
            {
                xmlDocument = LoadXmlDocument();
                notUsedNodes = GetNotUsedNodes(xmlDocument, settings);
                if (xmlDocument.DocumentElement != null)
                {
                    xmlDocument.DocumentElement.InnerXml = String.Empty;
                    xmlDocument.DocumentElement.SetAttribute("version", Settings.CurrentVersion.ToString());
                }
                else
                {
                    var rootEl = xmlDocument.CreateElement("Settings");
                    xmlDocument.AppendChild(rootEl);
                    rootEl.AppendAttribute("version", Settings.CurrentVersion.ToString());
                }
            }
            else
            {
                xmlDocument = new XmlDocument();
                var rootEl = xmlDocument.CreateElement("Settings");
                xmlDocument.AppendChild(rootEl);
                rootEl.AppendAttribute("version", Settings.CurrentVersion.ToString());
                notUsedNodes = (List<XmlNode>) settings.NotUsedSettings;
            }

            foreach (KeyValuePair<string, PropertyInfo> settingPair in settings.Properties)
            {
                var attr = (SettingItem)Attribute.GetCustomAttribute(settingPair.Value, typeof(SettingItem));
                if (settings.ContainsKey(attr.Key))
                {
                    SaveItem(xmlDocument, settings, attr.Key, settings[attr.Key], attr.SerializerMethodName);
                }
            }

            if (notUsedNodes != null)
            {
                notUsedNodes.ForEach(node => xmlDocument.ChildNodes[0].AppendChild(node));
            }

            return xmlDocument;
        }

        private void SaveItem(XmlDocument xmlDocument, Settings settings, String key, Object value, String serializerMethodName)
        {
            XmlElement rootEl = xmlDocument.DocumentElement;

            if (rootEl == null)
                throw new Exception("Xml document has no root element");

            XmlNode itemNode = xmlDocument.SelectSingleNode("descendant::item[@key='" + key + "']");
            //item is absent in the document
            if (itemNode == null)
            {
                itemNode = xmlDocument.CreateElement("item");
                rootEl.AppendChild(itemNode);
                itemNode.AppendAttribute("key", key);
                String strValue = null;

                if (String.IsNullOrEmpty(serializerMethodName))
                {
                    strValue = SerializeObject(value);
                }
                else
                {
                    MethodInfo methodInfo = settings.GetType().GetMethod(serializerMethodName, 
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

                    if (methodInfo != null)
                    {
                        strValue = methodInfo.Invoke(this, new[] { value }).ToString();
                    }
                }

                if (strValue != null)
                {
                    itemNode.InnerXml = strValue;
                }
                else
                {
                    itemNode.AppendAttribute("IsNull", "true");
                }
            }
            else
            {
                throw new Exception("Trying to add duplicated xml element");
            }
        }

        private static List<XmlNode> GetNotUsedNodes(XmlDocument xmlDocument, Settings settings)
        {
            if (!xmlDocument.HasChildNodes)
                return null;

            List<XmlNode> notUsedNodes = new List<XmlNode>();
            foreach (XmlElement itemNode in xmlDocument.DocumentElement.ChildNodes)
            {
                if (itemNode.HasAttribute("key"))
                {
                    String key = itemNode.Attributes["key"].Value;

                    Boolean found = settings.Properties.Keys.Contains(key);

                    if (!found)
                    {
                        notUsedNodes.Add(itemNode);
                    }
                }
            }

            return notUsedNodes;
        }

        /// <summary>
        /// Serializes an object using XmlSerializer
        /// </summary>
        /// <param name="item">An object to serialize</param>
        /// <returns>Xml that represents the serialized object</returns>
        public static String SerializeObject(Object item)
        {
            try
            {
                if (item == null)
                    return null;

                var sb = new StringBuilder();
                var xws = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    CloseOutput = true
                };
                using (var xw = XmlWriter.Create(sb, xws))
                {
                    var xs = new XmlSerializer(item.GetType());
                    xs.Serialize(xw, item);
                }
                return sb.ToString();

            }
            catch (Exception ex)
            {
                throw new SerializationException(string.Format("Cannot serialize type {0}.", item.GetType().FullName), ex);
            }            
        }

        private static Object DeserializeObject(Settings settings, Type type, String xml, String deserializerMethodName)
        {
            try
            {            
                if (String.IsNullOrEmpty(deserializerMethodName))
                {
                    return DeserializeObject(xml, type);
                }

                MethodInfo methodInfo = settings.GetType().GetMethod(deserializerMethodName, 
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

                return methodInfo != null ? methodInfo.Invoke(null, new[] { xml }) : null;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    string.Format("Cannot desirialize type {0} from xml:'{1}'. Deserializating method: {2}",
                                  type.FullName, xml, deserializerMethodName), ex);
            }
        }

        public static object DeserializeObject(string xml, Type type)
        {
            var xrs = new XmlReaderSettings
                          {
                              IgnoreComments = true,
                              ValidationType = ValidationType.None,
                              ValidationFlags = XmlSchemaValidationFlags.None
                          };

            using (var stream = new MemoryStream(Utility.StringToUTF8ByteArray(xml)))
            using (var xr = XmlReader.Create(stream, xrs))
            {
                return new XmlSerializer(type).Deserialize(xr);
            }
        }
    }
}
