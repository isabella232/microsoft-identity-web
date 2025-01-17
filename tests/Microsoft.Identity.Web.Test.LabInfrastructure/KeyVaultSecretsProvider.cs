﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace Microsoft.Identity.Web.Test.LabInfrastructure
{
    public class KeyVaultSecretsProvider : IDisposable
    {
        private const string BuildAutomationKeyVaultName = "https://buildautomation.vault.azure.net/";
        private const string MSIDLabLabKeyVaultName = "https://msidlabs.vault.azure.net";
        private readonly KeyVaultClient _keyVaultClient;

        /// <summary>Initialize the secrets provider with the "keyVault" configuration section.</summary>
        /// <remarks>
        /// <para>
        /// Authentication using <see cref="LabAccessAuthenticationType.ClientCertificate"/>
        ///     1. Register Azure AD application of "Web app / API" type.
        ///        To set up certificate based access to the application PowerShell should be used.
        ///     2. Add an access policy entry to target Key Vault instance for this application.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "ClientCertificate"
        ///         "clientId": [client ID]
        ///         "certThumbprint": [certificate thumbprint].
        /// </para>
        /// <para>
        /// Authentication using <see cref="LabAccessAuthenticationType.UserCredential"/>
        ///     1. Register Azure AD application of "Native" type.
        ///     2. Add to 'Required permissions' access to 'Azure Key Vault (AzureKeyVault)' API.
        ///     3. When you run your native client application, it will automatically prompt user to enter Azure AD credentials.
        ///     4. To successfully access keys/secrets in the Key Vault, the user must have specific permissions to perform those operations.
        ///        This could be achieved by directly adding an access policy entry to target Key Vault instance for this user
        ///        or an access policy entry for an Azure AD security group of which this user is a member of.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "UserCredential"
        ///         "clientId": [client ID].
        /// </para>
        /// </remarks>
        public KeyVaultSecretsProvider()
        {
            _keyVaultClient = new KeyVaultClient(AuthenticationCallbackAsync);
        }

        ~KeyVaultSecretsProvider()
        {
            Dispose();
        }

        public SecretBundle GetSecret(string secretUrl)
        {
            return _keyVaultClient.GetSecretAsync(secretUrl).GetAwaiter().GetResult();
        }

        public SecretBundle GetMsidLabSecret(string secretName)
        {
            return _keyVaultClient.GetSecretAsync(MSIDLabLabKeyVaultName, secretName).GetAwaiter().GetResult();
        }

        public X509Certificate2 GetCertificateWithPrivateMaterial(string certName)
        {
            SecretBundle secret = _keyVaultClient.GetSecretAsync(BuildAutomationKeyVaultName, certName).GetAwaiter().GetResult();
            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(secret.Value));
            return certificate;
        }

        public void Dispose()
        {
            _keyVaultClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<string> AuthenticationCallbackAsync(string authority, string resource, string scope)
        {
            return await LabAuthenticationHelper.GetLabAccessTokenAsync(authority, new[] { resource + "/.default" }).ConfigureAwait(false);
        }
    }
}
