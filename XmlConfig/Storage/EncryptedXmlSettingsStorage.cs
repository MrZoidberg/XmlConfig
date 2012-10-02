using System;
using System.IO;
using System.Xml;
using Mihmerk.XmlConfig.Crypto;

namespace Mihmerk.XmlConfig.Storage
{
    internal class EncryptedXmlSettingsStorage : XmlSettingsStorage
    {
        private readonly String _path;

        protected String Path
        {
            get
            {
                return _path;
            }
        }

        public string Password { get; set; }
        
        public EncryptedXmlSettingsStorage(String path, String password)
        {
            _path = path;
            Password = password;
        }

        protected override void CommitChanges(XmlDocument xmlDocument)
        {
            using (MemoryStream w = new MemoryStream())
            {
                xmlDocument.Save(w);
                using (XMLEncryptor enc = new XMLEncryptor(Password))
                {
                    enc.WriteEncryptedXML(w, Path);
                }
            }
        }

        public override XmlDocument LoadXmlDocument()
        {
            using (XMLEncryptor enc = new XMLEncryptor(Password))
            {
                XmlDocument xmlDocument = new XmlDocument();
                Stream r = enc.ReadEncryptedXML(Path);
                if (r != null)
                {
                    xmlDocument.Load(r);
                    r.Dispose();
                }
                else
                {
                    using (TextReader reader = new StreamReader(Path))
                    {
                        xmlDocument.Load(reader);
                    }
                }

                return xmlDocument;
            }
        }

        protected override bool Exists
        {
            get { return File.Exists(Path); }
        }
    }
}
