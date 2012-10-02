using System;
using System.IO;
using System.Xml;

namespace Mihmerk.XmlConfig.Storage
{
    internal class PlainXmlSettingsStorage : XmlSettingsStorage
    {
        private readonly String _path;

        protected String Path
        {
            get
            {
                return _path;
            }
        }

        public PlainXmlSettingsStorage(String path)
        {
            _path = path;
        }

        protected override void CommitChanges(XmlDocument xmlDocument)
        {
            using (FileStream w = new FileStream(_path,FileMode.OpenOrCreate))
            {
                xmlDocument.Save(w);               
            }
        }

        public override XmlDocument LoadXmlDocument()
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (TextReader reader = new StreamReader(Path))
            {
                xmlDocument.Load(reader);
            }

            return xmlDocument;
        }

        protected override bool Exists
        {
            get { return File.Exists(Path); }
        }
    }
}
