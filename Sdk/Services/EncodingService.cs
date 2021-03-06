﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using GPWebpayNet.Sdk.Exceptions;
using Microsoft.Extensions.Logging;

namespace GPWebpayNet.Sdk.Services
{
    /// <summary>
    /// Encoding service.
    /// </summary>
    /// <seealso cref="GPWebpayNet.Sdk.Services.IEncodingService" />
    public class EncodingService : IEncodingService
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingService"/> class.
        /// Needed to be called explicitelly in .netcore
        /// </summary>
        public EncodingService()
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public EncodingService(ILogger<EncodingService> logger) : this()
        {
            this.logger = logger;
        }


        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="privateCertificateFile">The certificate file.</param>
        /// <param name="privateCertificatePassword">The certificate password.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="keyStorageFlags">The key storage flags.</param>
        /// <returns>Signed mesage data (digest).</returns>
        /// <exception cref="GPWebpayNet.Sdk.Exceptions.SignDataException">
        /// No private key found - null
        /// or
        /// Error while signing data
        /// </exception>
        public string SignData(
            string message,
            string privateCertificateFile,
            string privateCertificatePassword,
            int encoding = Encoding.DefaultEncoding,
            X509KeyStorageFlags keyStorageFlags = Encoding.DefaultKeyStorageFlags)
        {
            try
            {
                var cert = new X509Certificate2(privateCertificateFile, privateCertificatePassword, keyStorageFlags);
                return this.SignData(message, cert, encoding);
            }
            catch (SignDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error while signing data: {ex.Message}\n{ex.StackTrace}");
                this.logger.LogError($"Message: {message}");
                this.logger.LogError($"Private ceritificate: {privateCertificateFile}");
                throw new SignDataException("Error while signing data", ex);
            }

        }

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="privateCertificate">The certificate.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>Signed mesage data (digest).</returns>
        /// <exception cref="GPWebpayNet.Sdk.Exceptions.SignDataException">
        /// No private key found - null
        /// or
        /// Error while signing data
        /// </exception>
        public string SignData(
            string message,
            X509Certificate2 privateCertificate,
            int encoding = Encoding.DefaultEncoding)
        {
            try
            {
                var msgData = System.Text.Encoding.GetEncoding(encoding).GetBytes(message);

                byte[] hash;
                using (var rsa = privateCertificate.GetRSAPrivateKey())
                {
                    if (rsa == null)
                    {
                        throw new SignDataException("No private key found", null);
                    }

                    hash = rsa.SignData(msgData, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }

                return Convert.ToBase64String(hash);
            }
            catch (SignDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error while signing data: {ex.Message}\n{ex.StackTrace}");
                this.logger.LogError($"Message: {message}");
                this.logger.LogError($"Private ceritificate: {privateCertificate.Thumbprint}");
                throw new SignDataException("Error while signing data", ex);
            }
        }

        /// <summary>
        /// Validates the digest.
        /// </summary>
        /// <param name="digest">The digest.</param>
        /// <param name="message">The message.</param>
        /// <param name="certificateFile">The certificate file.</param>
        /// <param name="certificatePassword">The certificate password.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="keyStorageFlags">The key storage flags.</param>
        /// <returns>Validation result.</returns>
        /// <exception cref="GPWebpayNet.Sdk.Exceptions.DigestValidationException">
        /// No pulic key found - null
        /// or
        /// Error while validating digest
        /// </exception>
        public bool ValidateDigest(
            string digest,
            string message,
            string certificateFile,
            string certificatePassword,
            int encoding = Encoding.DefaultEncoding,
            X509KeyStorageFlags keyStorageFlags = Encoding.DefaultKeyStorageFlags)
        {
            try
            {
                var cert = new X509Certificate2(certificateFile, certificatePassword, keyStorageFlags);
                return this.ValidateDigest(digest, message, cert, encoding);
            }
            catch (DigestValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error while validating digest: {ex.Message}\n{ex.StackTrace}");
                this.logger.LogError($"Digest: {digest}");
                this.logger.LogError($"Message: {message}");
                this.logger.LogError($"Public ceritificate: {certificateFile}");
                throw new DigestValidationException("Error while validating digest", ex);
            }

        }

        /// <summary>
        /// Validates the digest.
        /// </summary>
        /// <param name="digest">The digest.</param>
        /// <param name="message">The message.</param>
        /// <param name="publicCertificate">The certificate.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>Validation result.</returns>
        /// <exception cref="GPWebpayNet.Sdk.Exceptions.DigestValidationException">
        /// No pulic key found - null
        /// or
        /// Error while validating digest
        /// </exception>
        public bool ValidateDigest(
            string digest, string message,
            X509Certificate2 publicCertificate,
            int encoding = Encoding.DefaultEncoding)
        {
            try
            {
                var byteDigest = Convert.FromBase64String(digest);
                var data = System.Text.Encoding.GetEncoding(encoding).GetBytes(message);
                var sha = SHA1.Create();
                var hashResult = sha.ComputeHash(data);

                using (var rsa = publicCertificate.GetRSAPublicKey())
                {
                    if (rsa == null)
                    {
                        throw new DigestValidationException("No pulic key found", null);
                    }

                    return rsa.VerifyHash(hashResult, byteDigest, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }
            }
            catch (DigestValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error while validating digest: {ex.Message}\n{ex.StackTrace}");
                this.logger.LogError($"Digest: {digest}");
                this.logger.LogError($"Message: {message}");
                this.logger.LogError($"Public ceritificate: {publicCertificate.Thumbprint}");
                throw new DigestValidationException("Error while validating digest", ex);
            }
        }
    }
}