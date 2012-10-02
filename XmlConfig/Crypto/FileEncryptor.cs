using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mihmerk.XmlConfig.Crypto
{
    /// <summary>
    /// Provides methods for saving and loading the data (as <see cref="Stream"/>) to/from 
    /// the encrypted files.<br/>
    /// Limitations:
    /// <list type="bullet">
    /// <item><term>
    /// The class uses a user name and password to generate a combined key which is used for 
    /// encryption. No methods are provided to manage the secure storage of these data.
    /// </term></item>
    /// <item><term>
    /// Theoretically, the class can handle files up to 2GB in size. In practice, since 
    /// conversions are handled in memory to avoid having temporary (decrypted) files 
    /// being written to the drive, the practical size may be limited by available system 
    /// resources.
    /// </term></item>
    /// </list>
    /// </summary>
    internal class FileEncryptor: IDisposable
    {
        private const int BIN_SIZE = 4096;
        private readonly string _password;

        private byte[] _md5Iv;
        private byte[] _md5Key;
        private byte[] _signature;
        private readonly bool _validParameters;
        private readonly PaddingMode _paddingMode;

        private RijndaelManaged _rijn;
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="FileEncryptor"/> with
        /// specified password and <see cref="PaddingMode"/>.
        /// </summary>
        /// <param name="password"><see cref="String"/> representing a password that
        /// will be used for file encryption and decryption. Note that minimum password length 
        /// is 6 characters.</param>
        /// <param name="paddingMode"><see cref="PaddingMode"/> used by Rijndael encryption
        /// algoritm. It is recommended to use <see cref="PaddingMode.PKCS7"/> for binary
        /// files and <see cref="PaddingMode.Zeros"/> for text files.</param>
        public FileEncryptor(String password, PaddingMode paddingMode)
        {
            _password = password;
            _paddingMode = paddingMode;

            _validParameters = password.Length >= 6;

            if (!_validParameters)
            {
                // Abort the constructor. Calls to public functions will not work.
                return;
            }

            GenerateSignature();
            GenerateKey();
            GenerateIV();
        }

        #region Public Methods

        /// <summary>
        /// Reads encrypted file into the <see cref="CryptoStream"/>.
        /// </summary>
        /// <param name="fileName">The path to the file to dencrypt.</param>
        /// <returns><see cref="CryptoStream"/> linked to the encrypted file data.</returns>
        public CryptoStream ReadEncryptedFile(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            FileStream inFile;

            #region Check for possible errors (includes verification of the signature).

            if (!_validParameters)
            {
                Trace.WriteLine("Invalid parameters - cannot perform requested action");
                return null;
            }
            if (!fi.Exists)
            {
                Trace.WriteLine("Cannot perform decryption: File " + fileName + " does not exist.");
                return null;
            }
            if (fi.Length > Int32.MaxValue)
            {
                Trace.WriteLine("This decryption method can only handle files up to 2GB in size.");
                return null;
            }

            try
            {
                inFile = new FileStream(fileName, FileMode.Open);
            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.Message + "Cannot perform decryption");
                return null;
            }
            if (!VerifySignature(inFile))
            {
                inFile.Close();
                Trace.WriteLine("Invalid signature - file was not encrypted using this program");
                return null;
            }

            #endregion

            if (_rijn == null)
            {
                _rijn = new RijndaelManaged {Padding = _paddingMode};                
            }

            if (_decryptor == null)
            {
                _decryptor = _rijn.CreateDecryptor(_md5Key, _md5Iv);
            }

            // Allocate byte array buffer to read only the xml part of the file (ie everything following the signature).
            byte[] encryptedData = new byte[(int)fi.Length - _signature.Length];
            inFile.Position = _signature.Length;
            inFile.Read(encryptedData, 0, encryptedData.Length);

            // Convert the byte array to a MemoryStream object so that it can be passed on to the CryptoStream
            MemoryStream encryptedStream = new MemoryStream(encryptedData);
            // Create a CryptoStream, bound to the MemoryStream containing the encrypted xml data
            CryptoStream csDecrypt = new CryptoStream(encryptedStream, _decryptor, CryptoStreamMode.Read);
            
            // flush & close files.
            //encryptedXmlStream.Flush();
            //encryptedXmlStream.Close();
            inFile.Close();

            return csDecrypt;
        }

        /// <summary>
        /// Writes the <see cref="Stream"/> data to encrypted file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to encrypt.</param>
        /// <param name="encFileName">The path to encrypted file. Existing files will be overwritten.</param>
        public void WriteEncryptedFile(Stream stream, string encFileName)
        {
            #region Check for possible errors

            if (!_validParameters)
            {
                Trace.WriteLine("Invalid parameters - cannot perform requested action");
                return;
            }

            #endregion

            // Reset the pointer of the MemoryStream (which is at the EOF after the WriteXML function).
            stream.Position = 0;

            // Create a write FileStream and write the signature to it (unencrypted).
            FileStream fOut = new FileStream(encFileName, FileMode.Create);
            WriteSignature(fOut);

            #region Encryption objects

            if (_rijn == null)
            {
                _rijn = new RijndaelManaged { Padding = _paddingMode };
            }

            if (_encryptor == null)
            {
                _encryptor = _rijn.CreateEncryptor(_md5Key, _md5Iv);
            }
         
            CryptoStream csEncrypt = new CryptoStream(fOut, _encryptor, CryptoStreamMode.Write);

            #endregion

            //Create variables to help with read and write.
            byte[] bin = new byte[BIN_SIZE]; // Intermediate storage for the encryption.
            int rdlen = 0; // The total number of bytes written.
            int totlen = (int)stream.Length; // The total length of the input stream.
            int len; // The number of bytes to be written at a time.

            //Read from the input file, then encrypt and write to the output file.
            while (rdlen < totlen)
            {
                len = stream.Read(bin, 0, bin.Length);
                if (len == 0 && rdlen == 0)
                {
                    Trace.WriteLine("No read");
                    break;
                }
                csEncrypt.Write(bin, 0, len);
                rdlen += len;
            }
            csEncrypt.FlushFinalBlock();
            csEncrypt.Close();
            fOut.Close();
            stream.Close();
        }
        /*
        public ICryptoTransform GetEncryptor()
        {
            RijndaelManaged rijn = new RijndaelManaged {Padding = paddingMode};

            return rijn.CreateEncryptor(md5Key, md5IV);
        }
        */
        public ICryptoTransform GetDecryptor()
        {
            RijndaelManaged rijn = new RijndaelManaged {Padding = _paddingMode};
            
            return rijn.CreateDecryptor(_md5Key, _md5Iv);
        }

        #endregion

        #region Helper methods called from constructor only

        /// <summary>
        /// Generates a standard signature for the file.
        /// The signature may be longer than 16 bytes if deemed necessary.
        /// </summary>
        private void GenerateSignature()
        {
            _signature = new byte[]
                {
                    123, 078, 099, 166,
                    000, 043, 244, 008,
                    005, 089, 239, 255,
                    045, 188, 007, 033
                };
        }

        /// <summary>
        /// Generates an MD5 key for encryption/decryption. 
        /// This method is only called during construction.
        /// </summary>
        private void GenerateKey()
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            StringBuilder hash = new StringBuilder(_password);

            // Manipulate the hash string - not strictly necessary.
            for (int i = 1; i < hash.Length; i += 2)
            {
                char c = hash[i - 1];
                hash[i - 1] = hash[i];
                hash[i] = c;
            }

            // Convert the string into a byte array.
            Encoding unicode = Encoding.Unicode;
            byte[] unicodeBytes = unicode.GetBytes(hash.ToString());
            // Compute the key from the byte array
            _md5Key = md5.ComputeHash(unicodeBytes);
            md5.Clear();
        }

        /// <summary>
        /// Generates an MD5 Initialization Vector for encryption/decryption. 
        /// This method is only called during construction.
        /// </summary>
        private void GenerateIV()
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            string hash = _password;
            // Convert the string into a byte array.
            Encoding unicode = Encoding.Unicode;
            byte[] unicodeBytes = unicode.GetBytes(hash);

            // Compute the IV from the byte array
            _md5Iv = md5.ComputeHash(unicodeBytes);
            md5.Clear();
        }

        #endregion

        #region Methods to write and verify the signature

        private void WriteSignature(Stream fOut)
        {
            fOut.Position = 0;
            fOut.Write(_signature, 0, 16);
        }

        private Boolean VerifySignature(Stream fIn)
        {
            byte[] bin = new byte[16];
            fIn.Read(bin, 0, 16);
            for (int i = 0; i < 16; i++)
            {
                if (bin[i] != _signature[i])
                {
                    return false;
                }
            }
            // Reset file pointer.
            fIn.Position = 0;
            return true;
        }

        #endregion

        #region IDisposable Members

        // Track whether Dispose has been called.
        private bool _disposed;

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    if (_decryptor != null)
                    {
                        _decryptor.Dispose();
                        _decryptor = null;
                    }

                    if (_encryptor != null)
                    {
                        _encryptor.Dispose();
                        _encryptor = null;
                    }

                    if (_rijn != null)
                    {
                        _rijn.Clear();
                    }
                }

                // Disposing has been done.
                _disposed = true;

            }
        }

        #endregion
    }
}
