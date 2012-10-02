using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Mihmerk.XmlConfig.Crypto
{
    /// <summary>
    /// The XMLEncryptor class provides methods to save and read a DataSet or XML as an 
    /// encrypted XML file.<br/>
    /// Limitations:
    /// <list type="bullet">
    /// <item><term>
    /// The class uses a user name and password to generate a combined key which is used 
    /// for encryption. No methods are provided to manage the secure storage of these data.
    /// </term></item>
    /// <item><term>
    /// Theoretically, the class can handle files up to 2GB in size. In practice, since 
    /// conversions are handled in memory to avoid having temporary (decrypted) files 
    /// being written to the drive, the practical size may be limited by available system 
    /// resources.
    /// </term></item>
    /// </list>
    /// </summary>
    internal class XMLEncryptor : FileEncryptor
    {
        /// <summary>
        /// Creates and initializes a new instance of the <see cref="XMLEncryptor"/> with
        /// specified password.
        /// </summary>
        /// <param name="password"><see cref="String"/> representing a password that
        /// will be used for file encryption and decryption. Note that minimum password length 
        /// is 6 characters.</param>
        public XMLEncryptor(String password)
            : base(password, PaddingMode.Zeros)
        {

        }

        #region Public Methods

        /// <summary>
        /// Reads an encrypted XML file into a DataSet.
        /// </summary>
        /// <param name="dataSet">The DataSet.</param>
        /// <param name="fileName">The path to the XML file.</param>
        /// <returns>The DataSet, or null if an error occurs.</returns>
        public DataSet ReadEncryptedXML(DataSet dataSet, string fileName)
        {
            if (dataSet == null)
            {
                dataSet = new DataSet();
            }

            using (CryptoStream cryptoStream = ReadEncryptedFile(fileName))
            {
                try
                {
                    dataSet.ReadXml(cryptoStream, XmlReadMode.Auto);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Error decrypting XML");
                    dataSet.Clear();

                    throw;
                }
            }

            return dataSet;
        }

        public CryptoStream ReadEncryptedXML(string fileName)
        {
            return ReadEncryptedFile(fileName);
        }

        /// <summary>
        /// Writes a DataSet to the encrypted XML file.
        /// </summary>
        /// <param name="dataset">The DataSet to encrypt.</param>
        /// <param name="encFileName">The name of the encrypted file. Existing files will be overwritten.</param>
        public void WriteEncryptedXML(DataSet dataset, string encFileName)
        {
            // Create a MemoryStream and write the DataSet to it.
            using (MemoryStream xmlStream = new MemoryStream())
            {
                dataset.WriteXml(xmlStream);  
                WriteEncryptedFile(xmlStream, encFileName);
            }
        }

        /// <summary>
        /// Writes a XML from stream to an encrypted XML file.
        /// </summary>
        /// <param name="xmlStream">The stream.</param>
        /// <param name="encFileName">The name of the encrypted file. Existing files will be overwritten.</param>
        public void WriteEncryptedXML(Stream xmlStream, string encFileName)
        {
            WriteEncryptedFile(xmlStream, encFileName);
        }

        #endregion
    }
}